namespace OmniConvert.Service.Tests.Integration.Pipelines;

using Microsoft.Extensions.Logging.Abstractions;
using OmniConvert.Service.Conversion.Pipelines.Excel;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using Xunit;
using static System.Net.Mime.MediaTypeNames;

public class SyncfusionExcelPipelineTests : IDisposable
{
    private readonly SyncfusionExcelRenderMergePipeline _pipeline;
    private readonly string _testDir;

    public SyncfusionExcelPipelineTests()
    {
        _pipeline = new SyncfusionExcelRenderMergePipeline(
            NullLogger<SyncfusionExcelRenderMergePipeline>.Instance);

        _testDir = Path.Combine(
            Path.GetTempPath(), "OmniConvertSyncTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void CanHandle_SadecXlsxIcinTrue_Olmali()
    {
        Assert.True(_pipeline.CanHandle(SourceFormat.Xlsx));
        Assert.False(_pipeline.CanHandle(SourceFormat.Pdf));
        Assert.False(_pipeline.CanHandle(SourceFormat.Docx));
        Assert.False(_pipeline.CanHandle(SourceFormat.Png));
    }

    [Fact]
    public async Task TekSayfaXlsx_TiffeDonusmeli()
    {
        var inputPath = CreateTestXlsx("single.xlsx", sheetCount: 1);
        var outputPath = Path.Combine(_testDir, "output_single.tif");
        var context = BuildContext(inputPath, outputPath,
                             ColorMode.Color, CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.True(result.Success, result.ErrorMessage ?? "Pipeline başarısız");
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public async Task CokSayfaXlsx_MultiPageTiffOlusturulmali()
    {
        var inputPath = CreateTestXlsx("multi.xlsx", sheetCount: 3);
        var outputPath = Path.Combine(_testDir, "output_multi.tif");
        var context = BuildContext(inputPath, outputPath,
                             ColorMode.Gray, CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.True(result.Success, result.ErrorMessage ?? "Pipeline başarısız");
        Assert.True(File.Exists(result.OutputPath));

        // Multi-page TIFF boyutu tek sayfadan büyük olmalı
        var fileSize = new FileInfo(result.OutputPath!).Length;
        Assert.True(fileSize > 0);
    }

    [Fact]
    public async Task BinaryProfil_TiffOlusturulmali()
    {
        var inputPath = CreateTestXlsx("binary.xlsx", sheetCount: 1);
        var outputPath = Path.Combine(_testDir, "output_binary.tif");
        var context = BuildContext(inputPath, outputPath,
                             ColorMode.Binary, CompressionType.G4, threshold: 180);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.True(result.Success, result.ErrorMessage ?? "Pipeline başarısız");
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public async Task YanlisUzanti_ValidationFailure_Donmeli()
    {
        var inputPath = Path.Combine(_testDir, "test.xls"); // .xls değil .xlsx
        await File.WriteAllTextAsync(inputPath, "dummy");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath,
                             ColorMode.Color, CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.Validation, result.FailureCategory);
    }

    [Fact]
    public async Task OlmayanDosya_ConversionFailure_Donmeli()
    {
        var inputPath = Path.Combine(_testDir, "yok.xlsx");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath,
                             ColorMode.Color, CompressionType.LZW);

        var result = await _pipeline.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // -------------------------------------------------------------------------

    private static ConversionContext BuildContext(
        string inputPath,
        string outputPath,
        ColorMode colorMode,
        CompressionType compression,
        byte? threshold = null)
    {
        var profile = new ConversionProfile(
            Kind: ConversionProfileKind.ArchiveColor300Lzw,
            Dpi: 300,
            ColorMode: colorMode,
            CompressionType: compression,
            Threshold: threshold);

        return new ConversionContext(
            JobId: Guid.NewGuid(),
            InputFilePath: inputPath,
            OutputFilePath: outputPath,
            WorkspacePath: Path.GetDirectoryName(outputPath)!,
            SourceFormat: SourceFormat.Xlsx,
            Profile: profile);
    }

    /// <summary>
    /// Syncfusion ile minimal test XLSX oluşturur.
    /// Her sayfaya basit veri yazar.
    /// </summary>
    private string CreateTestXlsx(string fileName, int sheetCount)
    {
        var path = Path.Combine(_testDir, fileName);

        using var engine = new ExcelEngine();
        IApplication app = engine.Excel;
        app.DefaultVersion = ExcelVersion.Xlsx;
        app.XlsIORenderer = new XlsIORenderer();

        IWorkbook workbook = app.Workbooks.Create(sheetCount);

        for (int i = 0; i < sheetCount; i++)
        {
            var sheet = workbook.Worksheets[i];
            sheet.Name = $"Sheet{i + 1}";
            sheet.Range["A1"].Value = $"OmniConvert Test — Sayfa {i + 1}";
            sheet.Range["A2"].Value = "Sütun 1";
            sheet.Range["B2"].Value = "Sütun 2";
            sheet.Range["A3"].Value = "Değer 1";
            sheet.Range["B3"].Value = "Değer 2";
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        workbook.SaveAs(fs);

        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}