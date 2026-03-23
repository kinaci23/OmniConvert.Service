namespace OmniConvert.Service.Conversion.Pipelines.Word;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class LibreOfficeWordPdfBridgePipeline : IConversionPipeline
{
    public PipelineKind Kind => PipelineKind.LibreOfficeWordPdfBridge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Docx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }
}