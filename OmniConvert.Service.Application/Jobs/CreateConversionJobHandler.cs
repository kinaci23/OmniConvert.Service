namespace OmniConvert.Service.Application.Jobs;

using OmniConvert.Service.Core.Entities;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;

/// <summary>
/// Yeni bir ConversionJob oluşturur, depoya kaydeder ve kuyruğa ekler.
/// </summary>
public class CreateConversionJobHandler
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobQueue _jobQueue;
    private readonly IStorageService _storageService;
    private readonly IClock _clock;

    public CreateConversionJobHandler(
        IJobRepository jobRepository,
        IJobQueue jobQueue,
        IStorageService storageService,
        IClock clock)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
        _storageService = storageService;
        _clock = clock;
    }

    public async Task<ConversionJob> HandleAsync(
        string fileName,
        ConversionProfileKind profileKind,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();
        var sourceFormat = DetectFormat(fileName);
        var paths = await _storageService.PrepareJobStorageAsync(
                               jobId, fileName, cancellationToken);

        var job = new ConversionJob
        {
            Id = jobId,
            OriginalFileName = fileName,
            StoredInputPath = paths.InputPath,
            SourceFormat = sourceFormat,
            ProfileKind = profileKind,
            Status = JobStatus.Created,
            CreatedAtUtc = _clock.UtcNow
        };

        await _jobRepository.SaveAsync(job, cancellationToken);

        job.Status = JobStatus.Queued;
        job.QueuedAtUtc = _clock.UtcNow;
        await _jobQueue.EnqueueAsync(jobId, cancellationToken);
        await _jobRepository.SaveAsync(job, cancellationToken);

        return job;
    }

    /// <summary>
    /// Dosya uzantısını domain format ailesine eşler.
    /// jpg/jpeg → Jpeg, tif/tiff → Tiff olarak normalize edilir.
    /// </summary>
    private static SourceFormat DetectFormat(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();

        return ext switch
        {
            "docx" => SourceFormat.Docx,
            "xlsx" => SourceFormat.Xlsx,
            "pdf" => SourceFormat.Pdf,
            "jpg" or "jpeg" => SourceFormat.Jpeg,
            "png" => SourceFormat.Png,
            "tif" or "tiff" => SourceFormat.Tiff,
            _ => SourceFormat.Unknown
        };
    }
}