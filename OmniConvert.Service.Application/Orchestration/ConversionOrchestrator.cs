namespace OmniConvert.Service.Application.Orchestration;

using Microsoft.Extensions.Logging;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Core.Entities;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class ConversionOrchestrator : IConversionOrchestrator
{
    private readonly IJobRepository _jobRepository;
    private readonly IPipelineSelector _pipelineSelector;
    private readonly IEnumerable<IConversionPipeline> _pipelines;
    private readonly IOutputValidator _outputValidator;
    private readonly ConversionProfileResolver _profileResolver;
    private readonly ITempWorkspaceFactory _workspaceFactory;
    private readonly IClock _clock;
    private readonly ILogger<ConversionOrchestrator> _logger;

    public ConversionOrchestrator(
        IJobRepository jobRepository,
        IPipelineSelector pipelineSelector,
        IEnumerable<IConversionPipeline> pipelines,
        IOutputValidator outputValidator,
        ConversionProfileResolver profileResolver,
        ITempWorkspaceFactory workspaceFactory,
        IClock clock,
        ILogger<ConversionOrchestrator> logger)
    {
        _jobRepository = jobRepository;
        _pipelineSelector = pipelineSelector;
        _pipelines = pipelines;
        _outputValidator = outputValidator;
        _profileResolver = profileResolver;
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
            return new ConversionResult
            {
                JobId = jobId,
                Success = false,
                ErrorMessage = "İş bulunamadı.",
                FailureCategory = FailureCategory.Unknown
            };
        }

        var workspace = _workspaceFactory.CreateWorkspace(jobId);

        try
        {
            return await RunAsync(job, workspace, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("İş iptal edildi: {JobId}", jobId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen hata. Job: {JobId}", jobId);

            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.FailureCategory = FailureCategory.Unknown;
            job.CompletedAtUtc = _clock.UtcNow;

            try { await _jobRepository.SaveAsync(job, CancellationToken.None); }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Hata sonrası job kaydedilemedi: {JobId}", jobId);
            }

            return new ConversionResult
            {
                JobId = job.Id,
                Success = false,
                ErrorMessage = ex.Message,
                FailureCategory = FailureCategory.Unknown,
                PipelineUsed = job.SelectedPipeline
            };
        }
        finally
        {
            _workspaceFactory.CleanupWorkspace(jobId);
        }
    }

    // -------------------------------------------------------------------------

    private async Task<ConversionResult> RunAsync(
        ConversionJob job,
        string workspace,
        CancellationToken cancellationToken)
    {
        job.Status = JobStatus.Processing;
        job.StartedAtUtc = _clock.UtcNow;
        job.AttemptCount += 1;
        await _jobRepository.SaveAsync(job, cancellationToken);

        ConversionProfile profile;
        try
        {
            profile = _profileResolver.Resolve(
                job.ProfileKind,
                job.DpiOverride,
                job.ColorModeOverride,
                job.CompressionOverride);
        }
        catch (ArgumentException ex)
        {
            return await FailJobAsync(job, ex.Message,
                FailureCategory.Validation, null, cancellationToken);
        }

        _logger.LogInformation(
            "Profil çözümlendi: {Kind} | DPI={Dpi} | ColorMode={ColorMode} | " +
            "Compression={Compression} | Customized={Customized}",
            profile.Kind, profile.Dpi, profile.ColorMode,
            profile.CompressionType, profile.IsCustomized);

        PipelineSelectionResult selection;
        try
        {
            selection = _pipelineSelector.Select(job.SourceFormat);
        }
        catch (NotSupportedException ex)
        {
            return await FailJobAsync(job, ex.Message,
                FailureCategory.UnsupportedFormat, null, cancellationToken);
        }

        job.SelectedPipeline = selection.Primary;
        job.FallbackPipeline = selection.Fallback;
        await _jobRepository.SaveAsync(job, cancellationToken);

        if (string.IsNullOrWhiteSpace(job.StoredOutputPath))
            return await FailJobAsync(job, "Output path belirlenmemiş.",
                FailureCategory.Storage, null, cancellationToken);

        var context = new ConversionContext(
            job.Id, job.StoredInputPath, job.StoredOutputPath,
            workspace, job.SourceFormat, profile);

        // --- Birincil pipeline ---
        var primaryPipeline = ResolvePipeline(selection.Primary);
        if (primaryPipeline is null)
            return await FailJobAsync(job,
                $"Pipeline kayıtlı değil: {selection.Primary}",
                FailureCategory.Unknown, selection.Primary, cancellationToken);

        _logger.LogInformation("Birincil pipeline: {Pipeline} | Job: {JobId}",
            selection.Primary, job.Id);

        var primaryResult = await primaryPipeline.ExecuteAsync(context, cancellationToken);

        if (primaryResult.Success)
        {
            var validation = await ValidateOutputAsync(primaryResult.OutputPath, cancellationToken);
            if (validation.IsValid)
                return await CompleteJobAsync(job, primaryResult.OutputPath!, false,
                    selection.Primary, cancellationToken);

            _logger.LogWarning("Birincil pipeline çıktısı doğrulanamadı: {Reason} | Job: {JobId}",
                validation.Message, job.Id);
        }

        // --- Fallback pipeline ---
        if (selection.Fallback.HasValue)
        {
            _logger.LogWarning("Fallback deneniyor: {Fallback} | Job: {JobId}",
                selection.Fallback.Value, job.Id);

            var fallbackPipeline = ResolvePipeline(selection.Fallback.Value);
            if (fallbackPipeline is not null)
            {
                var fallbackResult = await fallbackPipeline.ExecuteAsync(context, cancellationToken);

                if (fallbackResult.Success)
                {
                    var validation = await ValidateOutputAsync(fallbackResult.OutputPath, cancellationToken);
                    if (validation.IsValid)
                        return await CompleteJobAsync(job, fallbackResult.OutputPath!, true,
                            selection.Fallback.Value, cancellationToken);

                    return await FailJobAsync(job,
                        $"Fallback çıktısı doğrulanamadı: {validation.Message}",
                        FailureCategory.Conversion, selection.Fallback.Value, cancellationToken);
                }

                return await FailJobAsync(job,
                    fallbackResult.ErrorMessage ?? "Fallback başarısız.",
                    ToFailureCategory(fallbackResult.FailureCategory),
                    selection.Fallback.Value, cancellationToken);
            }
        }

        var primaryError = primaryResult.Success
            ? "Çıktı TIFF doğrulamasından geçemedi."
            : primaryResult.ErrorMessage ?? "Birincil pipeline başarısız.";

        return await FailJobAsync(job, primaryError,
            ToFailureCategory(primaryResult.FailureCategory),
            selection.Primary, cancellationToken);
    }

    // -------------------------------------------------------------------------

    private IConversionPipeline? ResolvePipeline(PipelineKind kind)
        => _pipelines.FirstOrDefault(p => p.Kind == kind);

    private async Task<ValidationResult> ValidateOutputAsync(
        string? outputPath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return ValidationResult.Fail("Pipeline output path boş döndü.");

        return await _outputValidator.ValidateAsync(
            new OutputValidationContext(outputPath), ct);
    }

    private async Task<ConversionResult> CompleteJobAsync(
        ConversionJob job, string outputPath, bool usedFallback,
        PipelineKind pipelineUsed, CancellationToken cancellationToken)
    {
        job.Status = usedFallback ? JobStatus.CompletedWithFallback : JobStatus.Completed;
        job.OutputPath = outputPath;
        job.UsedFallback = usedFallback;
        job.CompletedAtUtc = _clock.UtcNow;
        await _jobRepository.SaveAsync(job, cancellationToken);

        _logger.LogInformation(
            "İş tamamlandı: {JobId} | Pipeline: {Pipeline} | Fallback: {Fallback}",
            job.Id, pipelineUsed, usedFallback);

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
        ConversionJob job, string errorMessage, FailureCategory failureCategory,
        PipelineKind? pipelineUsed, CancellationToken cancellationToken)
    {
        job.Status = JobStatus.Failed;
        job.ErrorMessage = errorMessage;
        job.FailureCategory = failureCategory;
        job.CompletedAtUtc = _clock.UtcNow;
        await _jobRepository.SaveAsync(job, cancellationToken);

        _logger.LogWarning(
            "İş başarısız: {JobId} | Kategori: {Category} | Mesaj: {Message}",
            job.Id, failureCategory, errorMessage);

        return new ConversionResult
        {
            JobId = job.Id,
            Success = false,
            ErrorMessage = errorMessage,
            FailureCategory = failureCategory,
            PipelineUsed = pipelineUsed
        };
    }

    private static FailureCategory ToFailureCategory(FailureCategory category)
        => category == FailureCategory.None ? FailureCategory.Conversion : category;
}