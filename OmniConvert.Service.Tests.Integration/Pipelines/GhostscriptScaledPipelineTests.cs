namespace OmniConvert.Service.Tests.Integration.Pipelines;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmniConvert.Service.Conversion.Configuration;
using OmniConvert.Service.Conversion.Pipelines.Pdf;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;
using OmniConvert.Service.Infrastructure.Processes;
using Xunit;

public class GhostscriptScaledPipelineTests : IDisposable
{
    private readonly string _testDir;

    public GhostscriptScaledPipelineTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(), "OmniConvertGsTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    /// <summary>
    /// Ghostscript kurulu ortamda gerçek dönüşüm testi.
    /// CI/CD ortamında Ghostscript kurulu değilse test skip edilir.
    /// </summary>
    [Fact]
    public async Task Pdf_Tiff_Donusumu_GhostscriptKuruluysaBasarili_Olmali()
    {
        var options = new GhostscriptOptions
        {
            Path = DetectGhostscriptPath(),
            TimeoutSeconds = 60
        };

        if (!File.Exists(options.Path))
        {
            // Ghostscript kurulu değil — testi atla
            return;
        }

        var pipeline = BuildPipeline(options);
        var inputPath = CreateMinimalPdf("test.pdf");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath,
                             ColorMode.Gray, CompressionType.LZW);

        var result = await pipeline.ExecuteAsync(context);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public async Task GhostscriptYok_ExternalProcessFail_Donmeli()
    {
        var options = new GhostscriptOptions
        {
            Path = Path.Combine(_testDir, "gs-yok.exe"),
            TimeoutSeconds = 30
        };

        var pipeline = BuildPipeline(options);
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(
            inputPath: Path.Combine(_testDir, "input.pdf"),
            outputPath: outputPath,
            colorMode: ColorMode.Gray,
            compression: CompressionType.LZW);

        var result = await pipeline.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.ExternalProcess, result.FailureCategory);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void CanHandle_SadecePdfIcinTrue_Donmeli()
    {
        var pipeline = BuildPipeline(new GhostscriptOptions());

        Assert.True(pipeline.CanHandle(SourceFormat.Pdf));
        Assert.False(pipeline.CanHandle(SourceFormat.Docx));
        Assert.False(pipeline.CanHandle(SourceFormat.Jpeg));
        Assert.False(pipeline.CanHandle(SourceFormat.Png));
    }

    // -------------------------------------------------------------------------

    private static GhostscriptScaledPipeline BuildPipeline(GhostscriptOptions options)
        => new(
            new ExternalProcessRunner(),
            Options.Create(options),
            NullLogger<GhostscriptScaledPipeline>.Instance);

    private static ConversionContext BuildContext(
        string inputPath,
        string outputPath,
        ColorMode colorMode,
        CompressionType compression)
    {
        var profile = new ConversionProfile(
            Kind: ConversionProfileKind.OcrGray300Lzw,
            Dpi: 300,
            ColorMode: colorMode,
            CompressionType: compression);

        return new ConversionContext(
            JobId: Guid.NewGuid(),
            InputFilePath: inputPath,
            OutputFilePath: outputPath,
            WorkspacePath: Path.GetDirectoryName(outputPath)!,
            SourceFormat: SourceFormat.Pdf,
            Profile: profile);
    }

    /// <summary>
    /// Minimal geçerli PDF — 1 sayfa, text içerikli.
    /// Ghostscript'in işleyebileceği en küçük PDF.
    /// </summary>
    private string CreateMinimalPdf(string fileName)
    {
        var path = Path.Combine(_testDir, fileName);
        var content = "%PDF-1.4\n" +
                      "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
                      "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
                      "3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj\n" +
                      "xref\n0 4\n" +
                      "0000000000 65535 f\r\n" +
                      "0000000009 00000 n\r\n" +
                      "0000000058 00000 n\r\n" +
                      "0000000115 00000 n\r\n" +
                      "trailer<</Size 4/Root 1 0 R>>\n" +
                      "startxref\n190\n%%EOF";
        File.WriteAllText(path, content);
        return path;
    }

    private static string DetectGhostscriptPath()
    {
        // Yaygın Windows kurulum yolları
        var candidates = new[]
        {
            @"C:\Program Files\gs\gs10.06.0\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.02.1\bin\gswin64c.exe",
        };

        foreach (var path in candidates)
            if (File.Exists(path)) return path;

        return candidates[0]; // bulunamazsa default — test skip olur
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}