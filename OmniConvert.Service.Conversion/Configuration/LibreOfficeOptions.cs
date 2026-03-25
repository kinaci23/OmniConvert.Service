namespace OmniConvert.Service.Conversion.Configuration;

public class LibreOfficeOptions
{
    public const string SectionName = "LibreOffice";

    /// <summary>
    /// LibreOffice executable yolu. Boşsa candidate path fallback devreye girer.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 180;
}