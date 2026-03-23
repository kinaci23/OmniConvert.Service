namespace OmniConvert.Service.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Contracts.Requests;
using OmniConvert.Service.Contracts.Responses;
using OmniConvert.Service.Core.Enums;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly CreateConversionJobHandler _createHandler;
    private readonly GetConversionJobStatusHandler _statusHandler;

    public JobsController(
        CreateConversionJobHandler createHandler,
        GetConversionJobStatusHandler statusHandler)
    {
        _createHandler = createHandler;
        _statusHandler = statusHandler;
    }

    /// <summary>Yeni bir dönüşüm işi oluşturur ve kuyruğa ekler.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateConversionJobRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new ErrorResponse("FileName zorunludur."));

        if (!Enum.TryParse<ConversionProfileKind>(request.ProfileKind, ignoreCase: true, out var profileKind))
            return BadRequest(new ErrorResponse($"Geçersiz profil türü: {request.ProfileKind}",
                "Geçerli değerler: OcrGray300Lzw, OcrBinary300G4, ArchiveColor300Lzw"));

        var job = await _createHandler.HandleAsync(request.FileName, profileKind, cancellationToken);

        return Accepted(new CreateConversionJobResponse(job.Id, job.Status.ToString(), job.OriginalFileName));
    }

    /// <summary>Belirli bir işin güncel durumunu döndürür.</summary>
    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetJob(Guid jobId, CancellationToken cancellationToken)
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