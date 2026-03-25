namespace OmniConvert.Service.Conversion.Pipelines.Excel;

using ImageMagick;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class SyncfusionExcelRenderMergePipeline : IConversionPipeline
{
    public PipelineKind Kind => PipelineKind.SyncfusionExcelRenderMerge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Xlsx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Syncfusion XlsIO entegrasyonu
        await Task.Delay(50, cancellationToken);
        WriteStubTiff(context.OutputFilePath);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }

    private static void WriteStubTiff(string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var image = new MagickImage(MagickColors.White, 16, 16);
        image.Format = MagickFormat.Tiff;
        image.Settings.Compression = CompressionMethod.LZW;
        image.Write(outputPath);
    }
}