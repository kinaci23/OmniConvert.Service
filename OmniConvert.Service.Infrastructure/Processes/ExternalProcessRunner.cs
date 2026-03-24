namespace OmniConvert.Service.Infrastructure.Processes;

using System.Diagnostics;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Dış süreçleri (Ghostscript, LibreOffice vb.) çalıştırır.
/// stdout/stderr yakalar, timeout ve cancellation destekler.
/// </summary>
public class ExternalProcessRunner : IExternalProcessRunner
{
    public async Task<ExternalProcessResult> RunAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        int timeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // stdout ve stderr paralel okunur — deadlock önlenir
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout — process'i öldür, caller'a timeout bildir
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Process timeout: {executable} {timeoutSeconds}s içinde tamamlanamadı.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new ExternalProcessResult(process.ExitCode, stdout, stderr);
    }
}