namespace OmniConvert.Service.Infrastructure.Configuration;

/// <summary>
/// Genel pipeline konfigürasyonu.
/// Ghostscript ayarları Conversion/Configuration/GhostscriptOptions içindedir.
/// </summary>
public class PipelineOptions
{
    public const string SectionName = "Pipelines";

    public string LibreOfficePath { get; set; } = "/usr/bin/libreoffice";
    public int LibreOfficeTimeoutSeconds { get; set; } = 180;
}