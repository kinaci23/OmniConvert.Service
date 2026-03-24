namespace OmniConvert.Service.Conversion.Pipelines.Excel;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class SyncfusionExcelRenderMergePipeline : IConversionPipeline
{
    // TODO: Syncfusion SDK bağımlılığı buraya enjekte edilecek
    // private readonly ExcelEngine _excelEngine;

    public PipelineKind Kind => PipelineKind.SyncfusionExcelRenderMerge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Xlsx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Syncfusion XlsIO ile Excel → TIFF dönüşümü
        // Her sayfayı ayrı TIFF olarak render et, sonra birleştir
        await Task.Delay(50, cancellationToken);
        await WriteStubOutputAsync(context.OutputFilePath, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }

    private static async Task WriteStubOutputAsync(string outputPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, "[stub: SyncfusionExcelRenderMerge]", ct);
    }
}