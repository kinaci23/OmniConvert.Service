namespace OmniConvert.Service.Conversion.Pipelines.Excel;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class LibreOfficeExcelPdfBridgePipeline : IConversionPipeline
{
    private readonly IExternalProcessRunner _processRunner;

    public LibreOfficeExcelPdfBridgePipeline(IExternalProcessRunner processRunner)
        => _processRunner = processRunner;

    public PipelineKind Kind => PipelineKind.LibreOfficeExcelPdfBridge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Xlsx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: LibreOffice Excel → PDF → TIFF zinciri
        await Task.Delay(50, cancellationToken);
        await WriteStubOutputAsync(context.OutputFilePath, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }

    private static async Task WriteStubOutputAsync(string outputPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, "[stub: LibreOfficeExcelPdfBridge]", ct);
    }
}