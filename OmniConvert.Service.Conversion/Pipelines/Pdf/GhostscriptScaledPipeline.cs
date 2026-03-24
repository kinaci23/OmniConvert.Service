namespace OmniConvert.Service.Conversion.Pipelines.Pdf;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniConvert.Service.Conversion.Configuration;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public sealed class GhostscriptScaledPipeline : IConversionPipeline
{
    private readonly IExternalProcessRunner _processRunner;
    private readonly GhostscriptOptions _options;
    private readonly ILogger<GhostscriptScaledPipeline> _logger;

    public GhostscriptScaledPipeline(
        IExternalProcessRunner processRunner,
        IOptions<GhostscriptOptions> options,
        ILogger<GhostscriptScaledPipeline> logger)
    {
        _processRunner = processRunner;
        _options = options.Value;
        _logger = logger;
    }

    public PipelineKind Kind => PipelineKind.GhostscriptScaled;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Pdf;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_options.Path))
        {
            _logger.LogError(
                "Ghostscript executable bulunamadı: {Path}", _options.Path);

            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"Ghostscript executable bulunamadı: {_options.Path}",
                FailureCategory: FailureCategory.ExternalProcess);
        }

        var device = ResolveGhostscriptDevice(context.Profile);
        var compression = ResolveCompressionArguments(context.Profile);
        var colorArgs = ResolveColorArguments(context.Profile);
        var arguments = BuildGhostscriptArguments(context, device, colorArgs, compression);

        _logger.LogInformation(
            "Ghostscript başlatılıyor | Job: {JobId} | Device: {Device} | " +
            "DPI: {Dpi} | Compression: {Compression} | Input: {Input} | Output: {Output}",
            context.JobId, device, context.Profile.Dpi, compression,
            context.InputFilePath, context.OutputFilePath);

        ExternalProcessResult result;
        try
        {
            result = await _processRunner.RunAsync(
                executable: _options.Path,
                arguments: arguments,
                workingDirectory: context.WorkspacePath,
                timeoutSeconds: _options.TimeoutSeconds,
                cancellationToken: cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(
                "Ghostscript timeout. Job: {JobId} | Limit: {Timeout}s",
                context.JobId, _options.TimeoutSeconds);

            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: ex.Message,
                FailureCategory: FailureCategory.Timeout);
        }

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "Ghostscript başarısız | Job: {JobId} | ExitCode: {ExitCode} | Stderr: {Stderr}",
                context.JobId, result.ExitCode, result.StandardError);

            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"Ghostscript exit code {result.ExitCode}: {result.StandardError}",
                FailureCategory: FailureCategory.ExternalProcess);
        }

        if (!File.Exists(context.OutputFilePath))
        {
            _logger.LogError(
                "Ghostscript başarılı döndü ama çıktı dosyası yok. Job: {JobId} | Path: {Path}",
                context.JobId, context.OutputFilePath);

            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"Ghostscript çıktı dosyası oluşturulamadı: {context.OutputFilePath}",
                FailureCategory: FailureCategory.Conversion);
        }

        _logger.LogInformation(
            "Ghostscript tamamlandı. Job: {JobId} | Output: {Output}",
            context.JobId, context.OutputFilePath);

        return new PipelineExecutionResult(
            Success: true,
            OutputPath: context.OutputFilePath);
    }

    // -------------------------------------------------------------------------
    // Benchmark mapping'i birebir korunuyor — değiştirme.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Benchmark: ResolveGhostscriptDevice
    /// Binary1Bit → tiffscaled
    /// Grayscale8Bit → tiffscaled8
    /// Rgb24Bit + Jpeg → tiff24nc
    /// Rgb24Bit + diğer → tiffscaled24
    /// </summary>
    private static string ResolveGhostscriptDevice(ConversionProfile profile)
    {
        if (profile.ColorMode == ColorMode.Binary)
            return "tiffscaled";

        if (profile.ColorMode == ColorMode.Gray)
            return "tiffscaled8";

        if (profile.ColorMode == ColorMode.Color)
            return "tiffscaled24";

        throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}");
    }

    /// <summary>
    /// Benchmark: ResolveCompressionArguments
    /// Lzw → -sCompression=lzw
    /// G4/Ccitt4 → -sCompression=g4
    /// </summary>
    private static string ResolveCompressionArguments(ConversionProfile profile)
    {
        return profile.CompressionType switch
        {
            CompressionType.LZW => "-sCompression=lzw",
            CompressionType.G4 => "-sCompression=g4",
            _ => throw new NotSupportedException(
                     $"Desteklenmeyen CompressionType: {profile.CompressionType}")
        };
    }

    /// <summary>
    /// Benchmark: ResolveColorArguments — boş string döndürüyor, korunuyor.
    /// </summary>
    private static string ResolveColorArguments(ConversionProfile profile) => string.Empty;

    /// <summary>
    /// Benchmark argüman sırası korunuyor:
    /// -dBATCH -dNOPAUSE -dSAFER -sDEVICE -r{dpi} {colorArgs} {compressionArgs} -sOutputFile input
    /// </summary>
    private static string BuildGhostscriptArguments(
        ConversionContext context,
        string device,
        string colorArgs,
        string compressionArg)
    {
        var outputPath = QuotePath(context.OutputFilePath);
        var inputPath = QuotePath(context.InputFilePath);

        // colorArgs boşsa fazladan boşluk bırakma
        var colorPart = string.IsNullOrWhiteSpace(colorArgs) ? string.Empty : $"{colorArgs} ";

        return $"-dBATCH " +
               $"-dNOPAUSE " +
               $"-dSAFER " +
               $"-sDEVICE={device} " +
               $"-r{context.Profile.Dpi} " +
               $"{colorPart}" +
               $"{compressionArg} " +
               $"-sOutputFile={outputPath} " +
               $"{inputPath}";
    }

    private static string QuotePath(string path)
        => path.Contains(' ') ? $"\"{path}\"" : path;
}