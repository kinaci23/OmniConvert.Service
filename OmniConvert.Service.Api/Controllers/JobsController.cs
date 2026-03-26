namespace OmniConvert.Service.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Contracts.Responses;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Infrastructure.Configuration;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly CreateConversionJobHandler _createHandler;
    private readonly GetConversionJobStatusHandler _statusHandler;
    private readonly ConversionProfileResolver _profileResolver;
    private readonly UploadOptions _uploadOptions;
    private readonly ILogger<JobsController> _logger;

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".docx", ".xlsx"
    ];

    public JobsController(
        CreateConversionJobHandler createHandler,
        GetConversionJobStatusHandler statusHandler,
        ConversionProfileResolver profileResolver,
        IOptions<UploadOptions> uploadOptions,
        ILogger<JobsController> logger)
    {
        _createHandler = createHandler;
        _statusHandler = statusHandler;
        _profileResolver = profileResolver;
        _uploadOptions = uploadOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Yeni bir dönüşüm işi oluşturur.
    /// Dosya multipart/form-data olarak gönderilir.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)] // 100 MB hard limit
    public async Task<IActionResult> CreateJob(
        IFormFile file,
        [FromForm] ConversionProfileKind profileKind,
        [FromForm] int? dpi = null,
        [FromForm] ColorMode? colorMode = null,
        [FromForm] CompressionType? compression = null,
        CancellationToken cancellationToken = default)
    {
        // --- Dosya validasyonu ---
        if (file is null || file.Length == 0)
            return BadRequest(new ErrorResponse("Dosya boş veya gönderilmedi."));

        if (file.Length > _uploadOptions.MaxFileSizeBytes)
            return BadRequest(new ErrorResponse(
                $"Dosya boyutu limiti aşıldı. Maksimum: {_uploadOptions.MaxFileSizeBytes / 1_048_576} MB"));

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new ErrorResponse(
                $"Desteklenmeyen dosya uzantısı: {ext}",
                $"İzin verilenler: {string.Join(", ", AllowedExtensions)}"));

        // --- Profil validasyonu ---
        try
        {
            _profileResolver.Resolve(profileKind, dpi, colorMode, compression);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse("Geçersiz profil parametresi.", ex.Message));
        }

        // --- Güvenli dosya adı ---
        var safeFileName = Path.GetFileName(file.FileName);

        _logger.LogInformation(
            "Upload alındı | FileName: {FileName} | Size: {Size} bytes | ProfileKind: {Profile}",
            safeFileName, file.Length, profileKind);

        // --- Job oluştur ve dosyayı storage'a yaz ---
        await using var stream = file.OpenReadStream();

        var job = await _createHandler.HandleAsync(
            safeFileName,
            stream,
            profileKind,
            dpi,
            colorMode,
            compression,
            cancellationToken);

        _logger.LogInformation(
            "Job oluşturuldu | JobId: {JobId} | FileName: {FileName} | " +
            "Format: {Format} | StoredPath: {Path}",
            job.Id, job.OriginalFileName, job.SourceFormat, job.StoredInputPath);

        return Accepted(new CreateConversionJobResponse(
            job.Id, job.Status.ToString(), job.OriginalFileName));
    }

    /// <summary>Belirli bir işin güncel durumunu döndürür.</summary>
    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetJob(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await _statusHandler.HandleAsync(jobId, cancellationToken);

        if (job is null)
            return NotFound(new ErrorResponse($"İş bulunamadı: {jobId}"));

        return Ok(new GetJobStatusResponse(
            job.Id,
            job.Status.ToString(),
            job.OriginalFileName,
            job.CreatedAtUtc,
            job.CompletedAtUtc,
            job.ErrorMessage));
    }
}