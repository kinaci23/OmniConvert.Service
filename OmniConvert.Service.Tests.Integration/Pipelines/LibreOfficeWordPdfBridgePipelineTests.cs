namespace OmniConvert.Service.Tests.Integration.Pipelines;

using ImageMagick;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmniConvert.Service.Conversion.Configuration;
using OmniConvert.Service.Conversion.Pipelines.Pdf;
using OmniConvert.Service.Conversion.Pipelines.Word;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;
using System.IO.Compression;
using Xunit;

public class LibreOfficeWordPdfBridgePipelineTests : IDisposable
{
    private readonly string _testDir;

    public LibreOfficeWordPdfBridgePipelineTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(), "OmniConvertLoTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void CanHandle_SadeceDosxIcinTrue_Olmali()
    {
        var pipeline = BuildPipeline(new FakeProcessRunner());

        Assert.True(pipeline.CanHandle(SourceFormat.Docx));
        Assert.False(pipeline.CanHandle(SourceFormat.Pdf));
        Assert.False(pipeline.CanHandle(SourceFormat.Xlsx));
        Assert.False(pipeline.CanHandle(SourceFormat.Jpeg));
    }

    [Fact]
    public async Task LibreOfficeYok_ExternalProcessFailure_Donmeli()
    {
        // Kesinlikle var olmayan path
        var pipeline = BuildPipeline(
            new FakeProcessRunner(),
            libreOfficePath: @"C:\kesinlikle\yok\soffice.exe");

        var inputPath = CreateMinimalDocx("test.docx");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath);

        var result = await pipeline.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.ExternalProcess, result.FailureCategory);
        Assert.Contains("bulunamadı", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DocxDisiUzanti_ValidationFailure_Donmeli()
    {
        var pipeline = BuildPipeline(new FakeProcessRunner());

        // .doc uzantısı — .docx değil, reddedilmeli
        var inputPath = Path.Combine(_testDir, "test.doc");
        await File.WriteAllTextAsync(inputPath, "dummy");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath);

        var result = await pipeline.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.Validation, result.FailureCategory);
    }

    [Fact]
    public async Task FakeRunner_TamZincir_TiffOlusturulmali()
    {
        // FakeProcessRunner:
        // - LibreOffice çağrısında → outdir'e minimal PDF yazar
        // - Ghostscript çağrısında → output path'e geçerli TIFF yazar
        var fakeRunner = new FakeProcessRunner();
        var pipeline = BuildPipeline(fakeRunner,
            libreOfficePath: Path.Combine(_testDir, "fake-soffice.exe"));

        // Fake executable var gibi göster
        await File.WriteAllTextAsync(
            Path.Combine(_testDir, "fake-soffice.exe"), "fake");

        var inputPath = CreateMinimalDocx("test.docx");
        var outputPath = Path.Combine(_testDir, "output.tif");
        var context = BuildContext(inputPath, outputPath);

        var result = await pipeline.ExecuteAsync(context);

        Assert.True(result.Success, result.ErrorMessage ?? "Pipeline başarısız oldu");
        Assert.True(File.Exists(result.OutputPath), "TIFF dosyası oluşmadı");
    }

    /// <summary>
    /// Gerçek LibreOffice varsa uçtan uca testi çalıştır.
    /// Kurulu değilse test geçer (skip).
    /// </summary>
    [Fact]
    public async Task GercekLibreOffice_KuruluysaDocxTiffeDonusmeli()
    {
        const string loPath =
            @"C:\Program Files\LibreOffice\program\soffice.exe";

        if (!File.Exists(loPath))
            return; // LibreOffice kurulu değil, testi atla

        var gsPath = DetectGhostscriptPath();
        if (!File.Exists(gsPath))
            return; // Ghostscript kurulu değil, testi atla

        var realRunner = new RealProcessRunner();
        var pipeline = BuildPipeline(realRunner,
            libreOfficePath: loPath, ghostscriptPath: gsPath);

        var inputPath = CreateMinimalDocx("real_test.docx");
        var outputPath = Path.Combine(_testDir, "real_output.tif");
        var context = BuildContext(inputPath, outputPath);

        var result = await pipeline.ExecuteAsync(context);

        Assert.True(result.Success, result.ErrorMessage ?? "Dönüşüm başarısız");
        Assert.True(File.Exists(result.OutputPath));
    }

    // -------------------------------------------------------------------------

    private LibreOfficeWordPdfBridgePipeline BuildPipeline(
        IExternalProcessRunner runner,
        string? libreOfficePath = null,
        string? ghostscriptPath = null)
    {
        var loOptions = Options.Create(new LibreOfficeOptions
        {
            Path = libreOfficePath ?? string.Empty,
            TimeoutSeconds = 30
        });

        var gsOptions = Options.Create(new GhostscriptOptions
        {
            Path = ghostscriptPath ?? DetectGhostscriptPath(),
            TimeoutSeconds = 60
        });

        var gsPipeline = new GhostscriptScaledPipeline(
            runner,
            gsOptions,
            NullLogger<GhostscriptScaledPipeline>.Instance);

        return new LibreOfficeWordPdfBridgePipeline(
            runner,
            gsPipeline,
            loOptions,
            NullLogger<LibreOfficeWordPdfBridgePipeline>.Instance);
    }

    private ConversionContext BuildContext(string inputPath, string outputPath)
    {
        var profile = new ConversionProfile(
            Kind: ConversionProfileKind.ArchiveColor300Lzw,
            Dpi: 300,
            ColorMode: ColorMode.Color,
            CompressionType: CompressionType.LZW);

        return new ConversionContext(
            JobId: Guid.NewGuid(),
            InputFilePath: inputPath,
            OutputFilePath: outputPath,
            WorkspacePath: _testDir,
            SourceFormat: SourceFormat.Docx,
            Profile: profile);
    }

    /// <summary>Minimal geçerli DOCX (ZIP tabanlı OpenXML).</summary>
    private string CreateMinimalDocx(string fileName)
    {
        var path = Path.Combine(_testDir, fileName);

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        Write(archive, "[Content_Types].xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                <Default Extension="xml"  ContentType="application/xml"/>
                <Override PartName="/word/document.xml"
                    ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            </Types>
            """);

        Write(archive, "_rels/.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                <Relationship Id="rId1"
                    Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"
                    Target="word/document.xml"/>
            </Relationships>
            """);

        Write(archive, "word/document.xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                <w:body>
                    <w:p><w:r><w:t>OmniConvert test document</w:t></w:r></w:p>
                </w:body>
            </w:document>
            """);

        Write(archive, "word/_rels/document.xml.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>
            """);

        return path;
    }

    private static void Write(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content.Trim());
    }

    private static string DetectGhostscriptPath()
    {
        string[] candidates =
        [
            @"C:\Program Files\gs\gs10.06.0\bin\gswin64c.exe",
            @"C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe"
        ];
        foreach (var p in candidates)
            if (File.Exists(p)) return p;
        return candidates[0];
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    // -------------------------------------------------------------------------
    // Fake ve real process runner implementasyonları
    // -------------------------------------------------------------------------

    /// <summary>
    /// LibreOffice çağrısında outdir'e PDF oluşturur.
    /// Ghostscript çağrısında output path'e geçerli TIFF oluşturur.
    /// </summary>
    private sealed class FakeProcessRunner : IExternalProcessRunner
    {
        public Task<ExternalProcessResult> RunAsync(
            string executable, string arguments,
            string? workingDirectory = null, int timeoutSeconds = 120,
            CancellationToken cancellationToken = default)
        {
            if (executable.Contains("soffice", StringComparison.OrdinalIgnoreCase))
                HandleLibreOfficeCall(arguments);
            else
                HandleGhostscriptCall(arguments);

            return Task.FromResult(new ExternalProcessResult(0, "[fake]", string.Empty));
        }

        private static void HandleLibreOfficeCall(string arguments)
        {
            // --outdir "path" → PDF oluştur
            var outdirTag = "--outdir \"";
            var start = arguments.IndexOf(outdirTag, StringComparison.Ordinal);
            if (start < 0) return;
            start += outdirTag.Length;
            var end = arguments.IndexOf('"', start);
            var outdir = arguments.Substring(start, end - start);

            // Input dosya adı → son quoted arg
            var lastQuote = arguments.LastIndexOf('"');
            var prevQuote = arguments.LastIndexOf('"', lastQuote - 1);
            var inputFile = arguments.Substring(prevQuote + 1, lastQuote - prevQuote - 1);
            var pdfName = Path.ChangeExtension(Path.GetFileName(inputFile), ".pdf");
            var pdfPath = Path.Combine(outdir, pdfName);

            Directory.CreateDirectory(outdir);
            CreateMinimalPdf(pdfPath);
        }

        private static void HandleGhostscriptCall(string arguments)
        {
            // -sOutputFile=path veya -sOutputFile="path"
            const string tag = "-sOutputFile=";
            var idx = arguments.IndexOf(tag, StringComparison.Ordinal);
            if (idx < 0) return;

            var rest = arguments.Substring(idx + tag.Length);
            string outputPath;

            if (rest.StartsWith('"'))
            {
                var closeQuote = rest.IndexOf('"', 1);
                outputPath = closeQuote > 0 ? rest.Substring(1, closeQuote - 1) : rest.Trim('"');
            }
            else
            {
                var spaceIdx = rest.IndexOf(' ');
                outputPath = spaceIdx > 0 ? rest.Substring(0, spaceIdx) : rest;
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            CreateValidTiff(outputPath);
        }

        private static void CreateMinimalPdf(string path)
        {
            // Minimal geçerli PDF — LibreOffice simülasyonu
            var content =
                "%PDF-1.4\n" +
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
        }

        private static void CreateValidTiff(string path)
        {
            // Magick.NET ile geçerli minimal TIFF
            using var image = new MagickImage(MagickColors.White, 16, 16);
            image.Format = MagickFormat.Tiff;
            image.Settings.Compression = CompressionMethod.LZW;
            image.Write(path);
        }
    }

    /// <summary>Gerçek process runner — real LibreOffice testi için.</summary>
    private sealed class RealProcessRunner : IExternalProcessRunner
    {
        private readonly OmniConvert.Service.Infrastructure.Processes.ExternalProcessRunner _inner = new();

        public Task<ExternalProcessResult> RunAsync(
            string executable, string arguments,
            string? workingDirectory = null, int timeoutSeconds = 120,
            CancellationToken cancellationToken = default)
            => _inner.RunAsync(executable, arguments, workingDirectory, timeoutSeconds, cancellationToken);
    }
}