namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.Entities;

public interface IConversionOrchestrator
{
    Task<ConversionResult> OrchestrateAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}