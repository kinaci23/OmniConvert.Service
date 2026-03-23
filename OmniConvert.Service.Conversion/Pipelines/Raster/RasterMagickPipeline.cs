namespace OmniConvert.Service.Conversion.Pipelines.Raster;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class RasterMagickPipeline : IConversionPipeline
{
    public PipelineKind Kind => PipelineKind.RasterMagick;

    public bool CanHandle(SourceFormat format)
        => format is SourceFormat.Jpg or SourceFormat.Jpeg
                  or SourceFormat.Png or SourceFormat.Tiff or SourceFormat.Tif;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }
}