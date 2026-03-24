namespace OmniConvert.Service.Conversion.Configuration;

/// <summary>
/// Ghostscript pipeline konfigürasyonu.
/// appsettings.json "Ghostscript" section'ından okunur.
/// </summary>
public class GhostscriptOptions
{
    public const string SectionName = "Ghostscript";

    public string Path { get; set; } = "gswin64c.exe";
    public int TimeoutSeconds { get; set; } = 120;
}