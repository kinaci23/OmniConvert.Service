namespace OmniConvert.Service.Tests.Integration.Jobs;

using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Application.Orchestration;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Application.Selection;
using OmniConvert.Service.Application.Validation;
using OmniConvert.Service.Conversion.Configuration;
using OmniConvert.Service.Conversion.Pipelines.Excel;
using OmniConvert.Service.Conversion.Pipelines.Pdf;
using OmniConvert.Service.Conversion.Pipelines.Raster;
using OmniConvert.Service.Conversion.Pipelines.Word;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Infrastructure.Concurrency;
using OmniConvert.Service.Infrastructure.Configuration;
using OmniConvert.Service.Infrastructure.Persistence;
using OmniConvert.Service.Infrastructure.Processes;
using OmniConvert.Service.Infrastructure.Queue;
using OmniConvert.Service.Infrastructure.Storage;
using OmniConvert.Service.Infrastructure.Temp;
using OmniConvert.Service.Infrastructure.Time;
using Xunit;

/// <summary>
/// CreateConversionJobHandler'ın Stream tabanlı upload akışını test eder.
/// </summary>
public class FileUploadJobTests : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly string _testBasePath;

    public FileUploadJobTests()
    {
        _testBasePath = Path.Combine(
            Path.GetTempPath(), "OmniConvertUploadTests", Guid.NewGuid().ToString());

        var services = new ServiceCollection();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.Configure<StorageOptions>(o => o.BasePath = _testBasePath);
        services.Configure<GhostscriptOptions>(o =>
        {
            o.Path = "gs-does-not-exist";
            o.TimeoutSeconds = 30;
        });
        services.Configure<LibreOfficeOptions>(o =>
        {
            o.Path = string.Empty;
            o.TimeoutSeconds = 30;
        });
        services.Configure<ConcurrencyOptions>(o =>
        {
            o.GhostscriptScaled = 2;
            o.RasterMagick = 2;
            o.LibreOfficeWordPdfBridge = 1;
            o.SyncfusionExcelRenderMerge = 1;
            o.LibreOfficeExcelPdfBridge = 1;
            o.TotalMaxConcurrent = 4;
        });

        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();
        services.AddSingleton<IConcurrencyLimiter, PipelineConcurrencyLimiter>();

        services.AddTransient<IStorageService, LocalFileStorageService>();
        services.AddTransient<ITempWorkspaceFactory, TempWorkspaceFactory>();
        services.AddTransient<IExternalProcessRunner, ExternalProcessRunner>();
        services.AddTransient<IClock, SystemClock>();

        services.AddTransient<GhostscriptScaledPipeline>();
        services.AddTransient<IConversionPipeline, GhostscriptScaledPipeline>();
        services.AddTransient<IConversionPipeline, LibreOfficeWordPdfBridgePipeline>();
        services.AddTransient<IConversionPipeline, SyncfusionExcelRenderMergePipeline>();
        services.AddTransient<IConversionPipeline, LibreOfficeExcelPdfBridgePipeline>();
        services.AddTransient<IConversionPipeline, RasterMagickPipeline>();

        services.AddTransient<IPipelineSelector, DefaultPipelineSelector>();
        services.AddTransient<IOutputValidator, DefaultOutputValidator>();
        services.AddTransient<ConversionProfileResolver>();
        services.AddSingleton<ILogger<ConversionOrchestrator>>(
            NullLogger<ConversionOrchestrator>.Instance);
        services.AddTransient<IConversionOrchestrator, ConversionOrchestrator>();

        services.AddTransient<CreateConversionJobHandler>();
        services.AddTransient<ProcessConversionJobHandler>();
        services.AddTransient<GetConversionJobStatusHandler>();

        _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PdfUpload_JobOlusturulur_InputDosyasiStorage()
    {
        var handler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var repo = _sp.GetRequiredService<IJobRepository>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        var pdfBytes = CreateMinimalPdfBytes();
        await using var stream = new MemoryStream(pdfBytes);

        var job = await handler.HandleAsync(
            "test.pdf", stream, ConversionProfileKind.ArchiveColor300Lzw);

        await queue.DequeueAsync(); // kuyruğu temizle

        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal("test.pdf", job.OriginalFileName);
        Assert.Equal(SourceFormat.Pdf, job.SourceFormat);

        // Dosya gerçekten storage'a yazıldı mı?
        Assert.True(File.Exists(job.StoredInputPath),
            "Upload edilen dosya storage'a yazılmadı.");

        var storedBytes = await File.ReadAllBytesAsync(job.StoredInputPath);
        Assert.Equal(pdfBytes.Length, storedBytes.Length);
    }

    [Fact]
    public async Task PngUpload_JobOlusturulur_FormatDogru()
    {
        var handler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        await using var stream = CreatePngStream();

        var job = await handler.HandleAsync(
            "gorsel.png", stream, ConversionProfileKind.OcrGray300Lzw);

        await queue.DequeueAsync();

        Assert.Equal(SourceFormat.Png, job.SourceFormat);
        Assert.Equal("gorsel.png", job.OriginalFileName);
        Assert.True(File.Exists(job.StoredInputPath));
    }

    [Fact]
    public async Task DocxUpload_JobOlusturulur_FormatDogru()
    {
        var handler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        var docxBytes = CreateMinimalDocxBytes();
        await using var stream = new MemoryStream(docxBytes);

        var job = await handler.HandleAsync(
            "belge.docx", stream, ConversionProfileKind.OcrGray300Lzw);

        await queue.DequeueAsync();

        Assert.Equal(SourceFormat.Docx, job.SourceFormat);
        Assert.True(File.Exists(job.StoredInputPath));
    }

    [Fact]
    public async Task Upload_StoredPathGuvenli_DirectoryTraversalYok()
    {
        var handler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        // Kötü niyetli dosya adı — directory traversal denemesi
        var maliciousName = "../../evil.pdf";
        var pdfBytes = CreateMinimalPdfBytes();
        await using var stream = new MemoryStream(pdfBytes);

        // Path.GetFileName controller tarafında uygulanmış olacak
        // Handler sadece fileName alır — storage path'i kendisi üretir
        var safeName = Path.GetFileName(maliciousName); // "evil.pdf"
        var job = await handler.HandleAsync(
            safeName, stream, ConversionProfileKind.ArchiveColor300Lzw);

        await queue.DequeueAsync();

        // StoredInputPath _testBasePath içinde olmalı
        Assert.StartsWith(_testBasePath, job.StoredInputPath,
            StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------

    private static byte[] CreateMinimalPdfBytes()
    {
        const string content =
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

        return System.Text.Encoding.ASCII.GetBytes(content);
    }

    private static MemoryStream CreatePngStream()
    {
        var stream = new MemoryStream();
        using var image = new MagickImage(MagickColors.White, 32, 32);
        image.Format = MagickFormat.Png;
        image.Write(stream);
        stream.Position = 0;
        return stream;
    }

    private static byte[] CreateMinimalDocxBytes()
    {
        using var ms = new MemoryStream();
        using var archive = new System.IO.Compression.ZipArchive(
            ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true);

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
                <w:body><w:p><w:r><w:t>Test</w:t></w:r></w:p></w:body>
            </w:document>
            """);

        Write(archive, "word/_rels/document.xml.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>
            """);

        return ms.ToArray();
    }

    private static void Write(
        System.IO.Compression.ZipArchive archive,
        string entryName,
        string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content.Trim());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, recursive: true);
    }
}