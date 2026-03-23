namespace OmniConvert.Service.Application.Jobs;

using OmniConvert.Service.Core.Entities;
using OmniConvert.Service.Core.Interfaces;

/// <summary>API katmanı için iş durumu sorgulama.</summary>
public class GetConversionJobStatusHandler
{
    private readonly IJobRepository _jobRepository;

    public GetConversionJobStatusHandler(IJobRepository jobRepository)
        => _jobRepository = jobRepository;

    public Task<ConversionJob?> HandleAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
        => _jobRepository.GetByIdAsync(jobId, cancellationToken);
}