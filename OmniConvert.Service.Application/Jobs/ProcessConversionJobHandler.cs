namespace OmniConvert.Service.Application.Jobs;

using OmniConvert.Service.Core.Entities;
using OmniConvert.Service.Core.Interfaces;

/// <summary>Worker tarafından çağrılır; işi orchestrator'a devreder.</summary>
public class ProcessConversionJobHandler
{
    private readonly IConversionOrchestrator _orchestrator;

    public ProcessConversionJobHandler(IConversionOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<ConversionResult> HandleAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
        => _orchestrator.OrchestrateAsync(jobId, cancellationToken);
}