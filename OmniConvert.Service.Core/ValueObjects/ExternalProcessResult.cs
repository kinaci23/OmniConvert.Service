namespace OmniConvert.Service.Core.ValueObjects;

/// <summary>Dış süreç çalıştırma sonucu.</summary>
public record ExternalProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError
)
{
    public bool Success => ExitCode == 0;
}