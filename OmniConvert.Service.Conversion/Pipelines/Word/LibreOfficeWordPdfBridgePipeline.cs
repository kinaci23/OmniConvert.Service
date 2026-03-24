namespace OmniConvert.Service.Conversion.Pipelines.Word;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class LibreOfficeWordPdfBridgePipeline : IConversionPipeline
{
    private readonly IExternalProcessRunner _processRunner;

    public LibreOfficeWordPdfBridgePipeline(IExternalProcessRunner processRunner)
        => _processRunner = processRunner;

    public PipelineKind Kind => PipelineKind.LibreOfficeWordPdfBridge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Docx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: LibreOffice entegrasyonu
        // Adım 1: DOCX → PDF (LibreOffice)
        // Adım 2: PDF → TIFF (Ghostscript veya ImageMagick)
        // var pdfPath = Path.ChangeExtension(context.OutputFilePath, ".pdf");
        // await _processRunner.RunAsync("soffice",
        //     $"--headless --convert-to pdf \"{context.InputFilePath}\" --outdir \"{context.WorkspacePath}\"",
        //     cancellationToken);

        await Task.Delay(50, cancellationToken);
        await WriteStubOutputAsync(context.OutputFilePath, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }

    private static async Task WriteStubOutputAsync(string outputPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, "[stub: LibreOfficeWordPdfBridge]", ct);
    }
}