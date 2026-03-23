namespace OmniConvert.Service.Infrastructure.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string BasePath { get; set; } =
        Path.Combine(Path.GetTempPath(), "OmniConvert", "jobs");
}