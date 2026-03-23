namespace OmniConvert.Service.Infrastructure.Configuration;

public class PipelineOptions
{
    public const string SectionName = "Pipelines";

    public int TimeoutSeconds { get; set; } = 120;
    public string LibreOfficePath { get; set; } = "/usr/bin/libreoffice";
    public string GhostscriptPath { get; set; } = "/usr/bin/gs";
}