namespace OmniConvert.Service.Tests.Integration.Pipelines;

using ImageMagick;
using Microsoft.Extensions.Logging.Abstractions;
using OmniConvert.Service.Conversion.Pipelines.Raster;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;
using Xunit;

public class RasterMagickPipelineTests : IDisposable
{
    private readonly RasterMagickPipeline _pipeline;
    private readonly string _testDir;

    public RasterMagickPipelineTests()
    {
        _pipeline = new RasterMagickPipeline(
            NullLogger<RasterMagickPipeline>.Instance);

        _testDir = Path.Combine(
            Path.GetTempPath(), "OmniConvertRasterTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task Png_Tiff_Donusumu_Basarili_Olmali()
    {
        var inputPath = CreateTestPng("test_input.png");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath, SourceFormat.Png,
                             ColorMode.Gray, CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.NotNull(result.OutputPath);
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public async Task Jpeg_Tiff_Donusumu_Basarili_Olmali()
    {
        var inputPath = CreateTestJpeg("test_input.jpg");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath, SourceFormat.Jpeg,
                             ColorMode.Color, CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.OutputPath!));
    }

    [Fact]
    public async Task Binary_Profil_Threshold_Uygulanmali()
    {
        var inputPath = CreateTestPng("test_binary.png");
        var outputPath = Path.Combine(_testDir, "output_binary.tif");
        var context = BuildContext(inputPath, outputPath, SourceFormat.Png,
                             ColorMode.Binary, CompressionType.G4, threshold: 180);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.OutputPath!));
    }

    [Fact]
    public async Task VarOlmayan_Input_HataliDonmeli()
    {
        var context = BuildContext(
            inputPath: Path.Combine(_testDir, "yok.png"),
            outputPath: Path.Combine(_testDir, "output.tif"),
            format: SourceFormat.Png,
            colorMode: ColorMode.Gray,
            compression: CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void CanHandle_DogruFormatlariKabulEtmeli()
    {
        Assert.True(_pipeline.CanHandle(SourceFormat.Jpeg));
        Assert.True(_pipeline.CanHandle(SourceFormat.Png));
        Assert.True(_pipeline.CanHandle(SourceFormat.Tiff));
        Assert.False(_pipeline.CanHandle(SourceFormat.Pdf));
        Assert.False(_pipeline.CanHandle(SourceFormat.Docx));
    }

    // -------------------------------------------------------------------------

    private static ConversionContext BuildContext(
        string inputPath,
        string outputPath,
        SourceFormat format,
        ColorMode colorMode,
        CompressionType compression,
        byte? threshold = null)
    {
        var profile = new ConversionProfile(
            Kind: ConversionProfileKind.OcrGray300Lzw,
            Dpi: 300,
            ColorMode: colorMode,
            CompressionType: compression,
            Threshold: threshold);

        return new ConversionContext(
            JobId: Guid.NewGuid(),
            InputFilePath: inputPath,
            OutputFilePath: outputPath,
            WorkspacePath: Path.GetDirectoryName(outputPath)!,
            SourceFormat: format,
            Profile: profile);
    }

    private string CreateTestPng(string fileName)
    {
        var path = Path.Combine(_testDir, fileName);
        using var image = new MagickImage(MagickColors.White, 64, 64);
        image.Format = MagickFormat.Png;
        image.Write(path);
        return path;
    }

    private string CreateTestJpeg(string fileName)
    {
        var path = Path.Combine(_testDir, fileName);
        using var image = new MagickImage(MagickColors.White, 64, 64);
        image.Format = MagickFormat.Jpeg;
        image.Write(path);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}