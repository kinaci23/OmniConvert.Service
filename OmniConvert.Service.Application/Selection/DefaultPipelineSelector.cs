namespace OmniConvert.Service.Application.Selection;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Format → pipeline eşlemesini uygular.
/// XLSX için birincil başarısız olduğunda devreye girecek yedek pipeline de belirlenir.
/// </summary>
public class DefaultPipelineSelector : IPipelineSelector
{
    public PipelineSelectionResult Select(SourceFormat format) => format switch
    {
        SourceFormat.Docx =>
            new(PipelineKind.LibreOfficeWordPdfBridge, Fallback: null),

        SourceFormat.Xlsx =>
            new(PipelineKind.SyncfusionExcelRenderMerge, Fallback: PipelineKind.LibreOfficeExcelPdfBridge),

        SourceFormat.Pdf =>
            new(PipelineKind.GhostscriptScaled, Fallback: null),

        SourceFormat.Jpg or SourceFormat.Jpeg or
        SourceFormat.Png or SourceFormat.Tiff or SourceFormat.Tif =>
            new(PipelineKind.RasterMagick, Fallback: null),

        _ => throw new NotSupportedException($"Bu format için pipeline tanımlı değil: {format}")
    };
}