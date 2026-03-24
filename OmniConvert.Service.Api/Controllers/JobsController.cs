namespace OmniConvert.Service.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Contracts.Requests;
using OmniConvert.Service.Contracts.Responses;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly CreateConversionJobHandler _createHandler;
    private readonly GetConversionJobStatusHandler _statusHandler;
    private readonly ConversionProfileResolver _profileResolver;

    public JobsController(
        CreateConversionJobHandler createHandler,
        GetConversionJobStatusHandler statusHandler,
        ConversionProfileResolver profileResolver)
    {
        _createHandler = createHandler;
        _statusHandler = statusHandler;
        _profileResolver = profileResolver;
    }

    /// <summary>
    /// Yeni bir dönüşüm işi oluşturur ve kuyruğa ekler.
    /// Enum değerleri JSON'da string olarak gönderilir.
    /// Örnek: "profileKind": "ArchiveColor300Lzw", "colorMode": "Gray"
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateConversionJobRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new ErrorResponse("FileName zorunludur."));

        // Erken validasyon — geçersiz kombinasyon kuyruğa alınmadan reddedilir
        try
        {
            _profileResolver.Resolve(
                request.ProfileKind,
                request.Dpi,
                request.ColorMode,
                request.Compression);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse("Geçersiz profil parametresi.", ex.Message));
        }

        var job = await _createHandler.HandleAsync(
            request.FileName,
            request.ProfileKind,
            request.Dpi,
            request.ColorMode,
            request.Compression,
            cancellationToken);

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