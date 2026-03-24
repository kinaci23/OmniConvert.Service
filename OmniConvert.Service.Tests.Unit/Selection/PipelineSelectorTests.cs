namespace OmniConvert.Service.Tests.Unit.Selection;

using OmniConvert.Service.Application.Selection;
using OmniConvert.Service.Core.Enums;
using Xunit;

public class PipelineSelectorTests
{
    private readonly DefaultPipelineSelector _selector = new();

    [Fact]
    public void Docx_PrimaryPipeline_LibreOfficeWordPdfBridge_Olmali()
    {
        var result = _selector.Select(SourceFormat.Docx);

        Assert.Equal(PipelineKind.LibreOfficeWordPdfBridge, result.Primary);
        Assert.Null(result.Fallback);
    }

    [Fact]
    public void Xlsx_PrimaryPipeline_Syncfusion_Fallback_LibreOffice_Olmali()
    {
        var result = _selector.Select(SourceFormat.Xlsx);

        Assert.Equal(PipelineKind.SyncfusionExcelRenderMerge, result.Primary);
        Assert.Equal(PipelineKind.LibreOfficeExcelPdfBridge, result.Fallback);
    }

    [Fact]
    public void Pdf_PrimaryPipeline_GhostscriptScaled_Olmali()
    {
        var result = _selector.Select(SourceFormat.Pdf);

        Assert.Equal(PipelineKind.GhostscriptScaled, result.Primary);
        Assert.Null(result.Fallback);
    }

    [Fact]
    public void Png_PrimaryPipeline_RasterMagick_Olmali()
    {
        var result = _selector.Select(SourceFormat.Png);

        Assert.Equal(PipelineKind.RasterMagick, result.Primary);
        Assert.Null(result.Fallback);
    }

    [Fact]
    public void Jpeg_PrimaryPipeline_RasterMagick_Olmali()
    {
        var result = _selector.Select(SourceFormat.Jpeg);

        Assert.Equal(PipelineKind.RasterMagick, result.Primary);
    }

    [Fact]
    public void Tiff_PrimaryPipeline_RasterMagick_Olmali()
    {
        var result = _selector.Select(SourceFormat.Tiff);

        Assert.Equal(PipelineKind.RasterMagick, result.Primary);
    }

    [Fact]
    public void UnknownFormat_NotSupportedException_Firlatmali()
    {
        Assert.Throws<NotSupportedException>(
            () => _selector.Select(SourceFormat.Unknown));
    }
}