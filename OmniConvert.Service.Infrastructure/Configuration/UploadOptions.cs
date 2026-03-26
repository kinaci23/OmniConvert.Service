namespace OmniConvert.Service.Infrastructure.Configuration;

public class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Maksimum upload boyutu (byte). Default 50 MB.</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;

    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".docx", ".xlsx"
    ];
}