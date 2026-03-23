namespace OmniConvert.Service.Conversion.Pipelines.Excel;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class LibreOfficeExcelPdfBridgePipeline : IConversionPipeline
{
    public PipelineKind Kind => PipelineKind.LibreOfficeExcelPdfBridge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Xlsx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }
}