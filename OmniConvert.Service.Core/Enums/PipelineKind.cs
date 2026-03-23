namespace OmniConvert.Service.Core.Enums;

public enum PipelineKind
{
    None = 0,
    LibreOfficeWordPdfBridge = 1,
    SyncfusionExcelRenderMerge = 2,
    LibreOfficeExcelPdfBridge = 3,
    GhostscriptScaled = 4,
    RasterMagick = 5
}