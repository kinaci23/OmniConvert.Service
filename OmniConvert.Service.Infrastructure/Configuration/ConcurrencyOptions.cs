namespace OmniConvert.Service.Infrastructure.Configuration;

/// <summary>
/// Pipeline bazlı ve toplam eş zamanlı iş limitleri.
/// appsettings.json "Concurrency" section'ından okunur.
/// </summary>
public class ConcurrencyOptions
{
    public const string SectionName = "Concurrency";

    public int GhostscriptScaled { get; set; } = 2;
    public int RasterMagick { get; set; } = 2;
    public int LibreOfficeWordPdfBridge { get; set; } = 1;
    public int SyncfusionExcelRenderMerge { get; set; } = 1;
    public int LibreOfficeExcelPdfBridge { get; set; } = 1;

    /// <summary>Tüm pipeline'lar dahil toplam aktif iş limiti.</summary>
    public int TotalMaxConcurrent { get; set; } = 4;
}