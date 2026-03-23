namespace OmniConvert.Service.Application.Orchestration;

using Microsoft.Extensions.Logging;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Core.Entities;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Bir conversion işinin tüm akışını yönetir:
/// iş yükleme → durum güncelleme → pipeline seçimi → çalıştırma →
/// doğrulama → gerekirse fallback → sonuç kaydetme.
/// </summary>
public class ConversionOrchestrator : IConversionOrchestrator
{
    private readonly IJobRepository _jobRepository;
    private readonly IPipelineSelector _pipelineSelector;
    private readonly IEnumerable<IConversionPipeline> _pipelines;
    private readonly IOutputValidator _outputValidator;
    private readonly ConversionProfileFactory _profileFactory;
    private readonly ITempWorkspaceFactory _workspaceFactory;
    private readonly IClock _clock;
    private readonly ILogger<ConversionOrchestrator> _logger;

    public ConversionOrchestrator(
        IJobRepository jobRepository,
        IPipelineSelector pipelineSelector,
        IEnumerable<IConversionPipeline> pipelines,
        IOutputValidator outputValidator,
        ConversionProfileFactory profileFactory,
        ITempWorkspaceFactory workspaceFactory,
        IClock clock,
        ILogger<ConversionOrchestrator> logger)
    {
        _jobRepository = jobRepository;
        _pipelineSelector = pipelineSelector;
        _pipelines = pipelines;
        _outputValidator = outputValidator;
        _profileFactory = profileFactory;
        _workspaceFactory = workspaceFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ConversionResult> OrchestrateAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogError("İş bulunamadı: {JobId}", jobId);
            return StaticFail(jobId, "İş bulunamadı.", FailureCategory.Unknown);
        }

        job.Status = JobStatus.Processing;
        job.StartedAtUtc = _clock.UtcNow;
        job.AttemptCount += 1;
        await _jobRepository.SaveAsync(job, cancellationToken);

        PipelineSelectionResult selection;
        try
        {
            selection = _pipelineSelector.Select(job.SourceFormat);
        }
        catch (NotSupportedException ex)
        {
            return await FailJobAsync(job, ex.Message, FailureCategory.UnsupportedFormat, PipelineKind.None, cancellationToken);
        }

        job.SelectedPipeline = selection.Primary;
        job.FallbackPipeline = selection.Fallback;
        await _jobRepository.SaveAsync(job, cancellationToken);

        var profile = _profileFactory.GetProfile(job.ProfileKind);
        var workspace = _workspaceFactory.CreateWorkspace(jobId);
        var outputPath = Path.Combine(workspace, $"{jobId}.tif");

        var context = new ConversionContext(
            job.Id,
            job.StoredInputPath,
            outputPath,
            workspace,
            job.SourceFormat,
            profile);

        // --- Birincil pipeline ---
        var primaryPipeline = ResolvePipeline(selection.Primary);
        if (primaryPipeline is null)
        {
            return await FailJobAsync(job,
                $"Pipeline kayıtlı değil: {selection.Primary}",
                FailureCategory.Unknown, selection.Primary, cancellationToken);
        }

        _logger.LogInformation("Birincil pipeline çalıştırılıyor: {Pipeline} | Job: {JobId}",
            selection.Primary, jobId);

        var primaryResult = await primaryPipeline.ExecuteAsync(context, cancellationToken);

        if (primaryResult.Success && await IsOutputValidAsync(primaryResult.OutputPath, cancellationToken))
            return await CompleteJobAsync(job, primaryResult.OutputPath!, usedFallback: false,
                selection.Primary, cancellationToken);

        // --- Yedek pipeline (yalnızca tanımlıysa) ---
        if (selection.Fallback.HasValue)
        {
            _logger.LogWarning("Birincil pipeline başarısız. Fallback deneniyor: {Fallback} | Job: {JobId}",
                selection.Fallback.Value, jobId);

            var fallbackPipeline = ResolvePipeline(selection.Fallback.Value);
            if (fallbackPipeline is not null)
            {
                var fallbackResult = await fallbackPipeline.ExecuteAsync(context, cancellationToken);

                if (fallbackResult.Success && await IsOutputValidAsync(fallbackResult.OutputPath, cancellationToken))
                    return await CompleteJobAsync(job, fallbackResult.OutputPath!, usedFallback: true,
                        selection.Fallback.Value, cancellationToken);

                return await FailJobAsync(job,
                    fallbackResult.ErrorMessage ?? "Fallback pipeline başarısız.",
                    fallbackResult.FailureCategory, selection.Fallback.Value, cancellationToken);
            }
        }

        return await FailJobAsync(job,
            primaryResult.ErrorMessage ?? "Birincil pipeline başarısız.",
            primaryResult.FailureCategory, selection.Primary, cancellationToken);
    }

    // -------------------------------------------------------------------------

    private IConversionPipeline? ResolvePipeline(PipelineKind kind)
        => _pipelines.FirstOrDefault(p => p.Kind == kind);

    private async Task<bool> IsOutputValidAsync(string? outputPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputPath)) return false;
        return await _outputValidator.ValidateAsync(new OutputValidationContext(outputPath), cancellationToken);
    }

    private async Task<ConversionResult> CompleteJobAsync(
        ConversionJob job,
        string outputPath,
        bool usedFallback,
        PipelineKind pipelineUsed,
        CancellationToken cancellationToken)
    {
        job.Status = usedFallback ? JobStatus.CompletedWithFallback : JobStatus.Completed;
        job.OutputPath = outputPath;
        job.UsedFallback = usedFallback;
        job.CompletedAtUtc = _clock.UtcNow;
        await _jobRepository.SaveAsync(job, cancellationToken);

        return new ConversionResult
        {
            JobId = job.Id,
            Success = true,
            OutputPath = outputPath,
            UsedFallback = usedFallback,
            PipelineUsed = pipelineUsed
        };
    }

    private async Task<ConversionResult> FailJobAsync(
        ConversionJob job,
        string errorMessage,
        FailureCategory failureCategory,
        PipelineKind pipelineUsed,
        CancellationToken cancellationToken)
    {
        job.Status = JobStatus.Failed;
        job.ErrorMessage = errorMessage;
        job.FailureCategory = failureCategory;
        job.CompletedAtUtc = _clock.UtcNow;
        await _jobRepository.SaveAsync(job, cancellationToken);

        return new ConversionResult
        {
            JobId = job.Id,
            Success = false,
            ErrorMessage = errorMessage,
            FailureCategory = failureCategory,
            PipelineUsed = pipelineUsed
        };
    }

    private static ConversionResult StaticFail(Guid jobId, string message, FailureCategory category)
        => new()
        {
            JobId = jobId,
            Success = false,
            ErrorMessage = message,
            FailureCategory = category,
            PipelineUsed = PipelineKind.None
        };
}