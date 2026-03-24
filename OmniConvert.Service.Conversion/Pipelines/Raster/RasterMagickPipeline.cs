namespace OmniConvert.Service.Conversion.Pipelines.Raster;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class RasterMagickPipeline : IConversionPipeline
{
    public PipelineKind Kind => PipelineKind.RasterMagick;

    public bool CanHandle(SourceFormat format)
        => format is SourceFormat.Jpeg or SourceFormat.Png or SourceFormat.Tiff;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        await WriteStubOutputAsync(context.OutputFilePath, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }

    private static async Task WriteStubOutputAsync(string outputPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, "[stub: RasterMagick]", ct);
    }
}