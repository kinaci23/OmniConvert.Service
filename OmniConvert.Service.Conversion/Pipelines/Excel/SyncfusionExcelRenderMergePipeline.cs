namespace OmniConvert.Service.Conversion.Pipelines.Excel;

using Microsoft.Extensions.Logging;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using Bitmap = System.Drawing.Bitmap;
using BitmapData = System.Drawing.Imaging.BitmapData;
using ColorMatrix = System.Drawing.Imaging.ColorMatrix;
using Encoder = System.Drawing.Imaging.Encoder;
using EncoderParameter = System.Drawing.Imaging.EncoderParameter;
using EncoderParameters = System.Drawing.Imaging.EncoderParameters;
using EncoderValue = System.Drawing.Imaging.EncoderValue;
using Graphics = System.Drawing.Graphics;
using GraphicsUnit = System.Drawing.GraphicsUnit;
// Explicit alias — ImplicitUsings'deki System.Net.Mime.MediaTypeNames.Image ile çakışmayı önler
using Image = System.Drawing.Image;
using ImageAttributes = System.Drawing.Imaging.ImageAttributes;
using ImageCodecInfo = System.Drawing.Imaging.ImageCodecInfo;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

public sealed class SyncfusionExcelRenderMergePipeline : IConversionPipeline
{
    private readonly ILogger<SyncfusionExcelRenderMergePipeline> _logger;

    public SyncfusionExcelRenderMergePipeline(
        ILogger<SyncfusionExcelRenderMergePipeline> logger)
        => _logger = logger;

    public PipelineKind Kind => PipelineKind.SyncfusionExcelRenderMerge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Xlsx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(context.InputFilePath);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"SyncfusionExcelRenderMerge sadece .xlsx destekler. Gelen: {ext}",
                FailureCategory: FailureCategory.Validation);
        }

        if (!File.Exists(context.InputFilePath))
        {
            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"Excel input dosyası bulunamadı: {context.InputFilePath}",
                FailureCategory: FailureCategory.Conversion);
        }

        var outputDir = Path.GetDirectoryName(context.OutputFilePath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        var createdImages = new List<Image>();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
            application.XlsIORenderer = new XlsIORenderer();

            using var inputStream = new FileStream(
                context.InputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            IWorkbook workbook = application.Workbooks.Open(inputStream);

            _logger.LogInformation(
                "Syncfusion rendering | Job: {JobId} | Input: {Input} | " +
                "Worksheets: {Count} | Output: {Output}",
                context.JobId, context.InputFilePath,
                workbook.Worksheets.Count, context.OutputFilePath);

            foreach (IWorksheet worksheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var imageStream = new MemoryStream();
                worksheet.ConvertToImage(worksheet.UsedRange, imageStream);

                imageStream.Position = 0;
                using var sourceImage = Image.FromStream(imageStream);

                Image prepared = PrepareFrame(sourceImage, context.Profile);
                createdImages.Add(prepared);
            }

            workbook.Close();

            if (createdImages.Count == 0)
            {
                return new PipelineExecutionResult(
                    Success: false,
                    OutputPath: null,
                    ErrorMessage: "Syncfusion hiçbir worksheet render edemedi.",
                    FailureCategory: FailureCategory.Conversion);
            }

            _logger.LogInformation(
                "Syncfusion frame'ler hazırlandı | Job: {JobId} | Frame: {Count}",
                context.JobId, createdImages.Count);

            SaveAsMultipageTiff(createdImages, context.OutputFilePath, context.Profile);

            if (!File.Exists(context.OutputFilePath))
            {
                return new PipelineExecutionResult(
                    Success: false,
                    OutputPath: null,
                    ErrorMessage: "Syncfusion çıktı TIFF dosyasını oluşturmadı.",
                    FailureCategory: FailureCategory.Conversion);
            }

            _logger.LogInformation(
                "Syncfusion tamamlandı | Job: {JobId} | Output: {Output}",
                context.JobId, context.OutputFilePath);

            return new PipelineExecutionResult(
                Success: true,
                OutputPath: context.OutputFilePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Syncfusion başarısız | Job: {JobId}", context.JobId);

            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: ex.Message,
                FailureCategory: FailureCategory.Conversion);
        }
        finally
        {
            foreach (var image in createdImages)
                image.Dispose();
        }

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Benchmark logic — değiştirme.
    // -------------------------------------------------------------------------

    private static Image PrepareFrame(Image source, ConversionProfile profile)
    {
        using var sourceBitmap = new Bitmap(source);

        Bitmap prepared = profile.ColorMode switch
        {
            ColorMode.Binary => ConvertToBinary1Bpp(sourceBitmap, profile.Threshold ?? 180),
            ColorMode.Gray => ConvertToGrayscale24Bpp(sourceBitmap),
            _ => new Bitmap(sourceBitmap)
        };

        prepared.SetResolution(profile.Dpi, profile.Dpi);
        return prepared;
    }

    private static Bitmap ConvertToGrayscale24Bpp(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(result);
        using var attributes = new ImageAttributes();

        var colorMatrix = new ColorMatrix(
        [
            [0.299f, 0.299f, 0.299f, 0f, 0f],
            [0.587f, 0.587f, 0.587f, 0f, 0f],
            [0.114f, 0.114f, 0.114f, 0f, 0f],
            [0f,     0f,     0f,     1f, 0f],
            [0f,     0f,     0f,     0f, 1f]
        ]);

        attributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, result.Width, result.Height),
            0, 0, source.Width, source.Height,
            GraphicsUnit.Pixel,
            attributes);

        return result;
    }

    private static Bitmap ConvertToBinary1Bpp(Bitmap source, int threshold)
    {
        using Bitmap gray = ConvertToGrayscale24Bpp(source);

        int width = gray.Width;
        int height = gray.Height;

        var binary = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
        var grayRect = new Rectangle(0, 0, width, height);
        var binRect = new Rectangle(0, 0, width, height);

        BitmapData grayData = gray.LockBits(
            grayRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        BitmapData binData = binary.LockBits(
            binRect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        try
        {
            int grayStride = grayData.Stride;
            int binStride = binData.Stride;
            byte[] grayBytes = new byte[grayStride * height];
            byte[] binBytes = new byte[binStride * height];

            Marshal.Copy(grayData.Scan0, grayBytes, 0, grayBytes.Length);

            for (int y = 0; y < height; y++)
            {
                int grayRow = y * grayStride;
                int binRow = y * binStride;

                for (int x = 0; x < width; x++)
                {
                    byte grayValue = grayBytes[grayRow + (x * 3)];
                    if (grayValue < threshold)
                        binBytes[binRow + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                }
            }

            Marshal.Copy(binBytes, 0, binData.Scan0, binBytes.Length);
        }
        finally
        {
            gray.UnlockBits(grayData);
            binary.UnlockBits(binData);
        }

        return binary;
    }

    private static void SaveAsMultipageTiff(
        IReadOnlyList<Image> images,
        string outputPath,
        ConversionProfile profile)
    {
        var tiffEncoder = GetEncoderInfo("image/tiff")
            ?? throw new InvalidOperationException("TIFF encoder bulunamadı.");

        using var encoderParameters = new EncoderParameters(2);
        encoderParameters.Param[0] = new EncoderParameter(
            Encoder.Compression, (long)MapCompression(profile));
        encoderParameters.Param[1] = new EncoderParameter(
            Encoder.SaveFlag, (long)EncoderValue.MultiFrame);

        using Image firstFrame = (Image)images[0].Clone();
        firstFrame.Save(outputPath, tiffEncoder, encoderParameters);

        for (int i = 1; i < images.Count; i++)
        {
            using Image nextFrame = (Image)images[i].Clone();
            encoderParameters.Param[1] = new EncoderParameter(
                Encoder.SaveFlag, (long)EncoderValue.FrameDimensionPage);
            firstFrame.SaveAdd(nextFrame, encoderParameters);
        }

        encoderParameters.Param[1] = new EncoderParameter(
            Encoder.SaveFlag, (long)EncoderValue.Flush);
        firstFrame.SaveAdd(encoderParameters);
    }

    private static EncoderValue MapCompression(ConversionProfile profile)
    {
        return profile.CompressionType switch
        {
            CompressionType.G4 => EncoderValue.CompressionCCITT4,
            CompressionType.LZW => EncoderValue.CompressionLZW,
            CompressionType.Jpeg => EncoderValue.CompressionLZW,
            _ => EncoderValue.CompressionLZW
        };
    }

    private static ImageCodecInfo? GetEncoderInfo(string mimeType)
        => ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c =>
                c.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase));
}