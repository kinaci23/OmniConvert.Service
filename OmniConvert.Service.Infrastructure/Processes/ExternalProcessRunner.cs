namespace OmniConvert.Service.Infrastructure.Processes;

using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Stub uygulama. Gerçek süreç çalıştırma mantığı ileriki iterasyonda eklenir.
/// </summary>
public class ExternalProcessRunner : IExternalProcessRunner
{
    public Task<ExternalProcessResult> RunAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var result = new ExternalProcessResult(
            ExitCode: 0,
            StandardOutput: "[stub] process simulated",
            StandardError: string.Empty);

        return Task.FromResult(result);
    }
}