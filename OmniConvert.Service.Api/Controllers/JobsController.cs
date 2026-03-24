namespace OmniConvert.Service.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Contracts.Requests;
using OmniConvert.Service.Contracts.Responses;
using OmniConvert.Service.Core.Enums;

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
    /// Sadece preset, ya da preset + override kombinasyonu gönderilebilir.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateConversionJobRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new ErrorResponse("FileName zorunludur."));

        if (string.IsNullOrWhiteSpace(request.ProfileKind))
            return BadRequest(new ErrorResponse("ProfileKind zorunludur."));

        if (!Enum.TryParse<ConversionProfileKind>(
                request.ProfileKind, ignoreCase: true, out var profileKind))
        {
            return BadRequest(new ErrorResponse(
                $"Geçersiz profil türü: {request.ProfileKind}",
                "Geçerli değerler: OcrGray300Lzw, OcrBinary300G4, ArchiveColor300Lzw"));
        }

        // Erken validasyon — geçersiz kombinasyon kuyruğa alınmadan reddedilir
        try
        {
            _profileResolver.Resolve(
                profileKind, request.Dpi, request.ColorMode, request.Compression);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse("Geçersiz profil parametresi.", ex.Message));
        }

        var job = await _createHandler.HandleAsync(
            request.FileName, profileKind,
            request.Dpi, request.ColorMode, request.Compression,
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