namespace OmniConvert.Service.Conversion.Pipelines.Pdf;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public class GhostscriptScaledPipeline : IConversionPipeline
{
    private readonly IExternalProcessRunner _processRunner;

    public GhostscriptScaledPipeline(IExternalProcessRunner processRunner)
        => _processRunner = processRunner;

    public PipelineKind Kind => PipelineKind.GhostscriptScaled;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Pdf;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Ghostscript entegrasyonu
        // var args = BuildArguments(context);
        // var result = await _processRunner.RunAsync("gs", args, cancellationToken);
        // if (!result.Success) return new PipelineExecutionResult(false, null,
        //     result.StandardError, FailureCategory.ExternalProcess);

        await Task.Delay(50, cancellationToken);
        await WriteStubOutputAsync(context.OutputFilePath, cancellationToken);
        return new PipelineExecutionResult(Success: true, OutputPath: context.OutputFilePath);
    }

    // Future:
    // private static string BuildArguments(ConversionContext ctx) =>
    //     $"-dNOPAUSE -dBATCH -sDEVICE=tiff{ctx.Profile.CompressionType.ToLower()} " +
    //     $"-r{ctx.Profile.Dpi} -sOutputFile=\"{ctx.OutputFilePath}\" \"{ctx.InputFilePath}\"";

    private static async Task WriteStubOutputAsync(string outputPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, "[stub: GhostscriptScaled]", ct);
    }
}