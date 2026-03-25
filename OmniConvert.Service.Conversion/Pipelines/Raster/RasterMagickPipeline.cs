namespace OmniConvert.Service.Conversion.Pipelines.Raster;

using ImageMagick;
using Microsoft.Extensions.Logging;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public sealed class RasterMagickPipeline : IConversionPipeline
{
    private readonly ILogger<RasterMagickPipeline> _logger;

    public RasterMagickPipeline(ILogger<RasterMagickPipeline> logger)
        => _logger = logger;

    public PipelineKind Kind => PipelineKind.RasterMagick;

    public bool CanHandle(SourceFormat format)
        => format is SourceFormat.Jpeg or SourceFormat.Png or SourceFormat.Tiff;

    public Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "RasterMagick başlatılıyor | Job: {JobId} | DPI: {Dpi} | " +
            "ColorMode: {ColorMode} | Compression: {Compression} | " +
            "Input: {Input} | Output: {Output}",
            context.JobId, context.Profile.Dpi,
            context.Profile.ColorMode, context.Profile.CompressionType,
            context.InputFilePath, context.OutputFilePath);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var image = new MagickImage(context.InputFilePath);
            ApplyProfile(image, context.Profile);
            image.Write(context.OutputFilePath);

            if (!File.Exists(context.OutputFilePath))
            {
                _logger.LogError(
                    "RasterMagick çıktı dosyası oluşturulamadı. Job: {JobId} | Path: {Path}",
                    context.JobId, context.OutputFilePath);

                return Task.FromResult(new PipelineExecutionResult(
                    Success: false,
                    OutputPath: null,
                    ErrorMessage: $"Çıktı dosyası oluşturulamadı: {context.OutputFilePath}",
                    FailureCategory: FailureCategory.Conversion));
            }

            _logger.LogInformation(
                "RasterMagick tamamlandı. Job: {JobId} | Output: {Output}",
                context.JobId, context.OutputFilePath);

            return Task.FromResult(new PipelineExecutionResult(
                Success: true,
                OutputPath: context.OutputFilePath));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RasterMagick başarısız. Job: {JobId}", context.JobId);

            return Task.FromResult(new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: ex.Message,
                FailureCategory: FailureCategory.Conversion));
        }
    }

    // -------------------------------------------------------------------------
    // Benchmark: ApplyProfile — birebir korunuyor.
    // -------------------------------------------------------------------------

    private static void ApplyProfile(MagickImage image, ConversionProfile profile)
    {
        image.Density = new Density(profile.Dpi, profile.Dpi);
        image.Format = MagickFormat.Tiff;

        switch (profile.ColorMode)
        {
            case ColorMode.Binary:
                image.Grayscale();
                if (profile.Threshold.HasValue)
                    image.Threshold(new Percentage(profile.Threshold.Value / 255.0 * 100.0));
                else
                    image.Threshold(new Percentage(50));
                break;

            case ColorMode.Gray:
                image.Grayscale();
                break;

            case ColorMode.Color:
                break;

            default:
                throw new NotSupportedException(
                    $"Desteklenmeyen ColorMode: {profile.ColorMode}");
        }

        switch (profile.CompressionType)
        {
            case CompressionType.None:
                image.Settings.Compression = CompressionMethod.NoCompression;
                break;

            case CompressionType.LZW:
                image.Settings.Compression = CompressionMethod.LZW;
                break;

            case CompressionType.Jpeg:
                image.Settings.Compression = CompressionMethod.JPEG;
                image.Quality = (uint)(profile.JpegQuality ?? 85);
                break;

            case CompressionType.G4:
                image.Settings.Compression = CompressionMethod.Group4;
                break;

            default:
                throw new NotSupportedException(
                    $"Desteklenmeyen CompressionType: {profile.CompressionType}");
        }
    }
}