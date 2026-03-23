namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.ValueObjects;

public interface IExternalProcessRunner
{
    Task<ExternalProcessResult> RunAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken = default);
}