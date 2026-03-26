namespace OmniConvert.Service.Tests.Integration.Jobs;

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

public class JobProcessingFlowTests : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly string _testBasePath;

    public JobProcessingFlowTests()
    {
        _testBasePath = Path.Combine(
            Path.GetTempPath(), "OmniConvertTests", Guid.NewGuid().ToString());

        var services = new ServiceCollection();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.Configure<StorageOptions>(o => o.BasePath = _testBasePath);
        services.Configure<GhostscriptOptions>(o =>
        {
            o.Path = "gs-stub-does-not-exist";
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
    public async Task PdfIsi_IslendigindeTamamlanmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = _sp.GetRequiredService<ProcessConversionJobHandler>();
        var statusHandler = _sp.GetRequiredService<GetConversionJobStatusHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        await using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var job = await createHandler.HandleAsync(
            "rapor.pdf", stream, ConversionProfileKind.ArchiveColor300Lzw);

        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.NotEmpty(job.StoredOutputPath);

        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.ExternalProcess, result.FailureCategory);

        var finalJob = await statusHandler.HandleAsync(jobId);
        Assert.NotNull(finalJob);
        Assert.Equal(JobStatus.Failed, finalJob!.Status);
        Assert.NotNull(finalJob.ErrorMessage);
    }

    [Fact]
    public async Task DocxIsi_LibreOfficePipelineKullanmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = _sp.GetRequiredService<ProcessConversionJobHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        await using var stream = new MemoryStream(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
        var job = await createHandler.HandleAsync(
            "belge.docx", stream, ConversionProfileKind.OcrGray300Lzw);
        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.False(result.Success);
        Assert.Equal(PipelineKind.LibreOfficeWordPdfBridge, result.PipelineUsed);
    }

    [Fact]
    public async Task DpiOverrideIle_IsOlusturulunca_OverrideSaklanmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var repo = _sp.GetRequiredService<IJobRepository>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        await using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var job = await createHandler.HandleAsync(
            "arsiv.pdf", stream, ConversionProfileKind.ArchiveColor300Lzw,
            dpiOverride: 600);

        await queue.DequeueAsync();

        var saved = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(saved);
        Assert.Equal(600, saved!.DpiOverride);
        Assert.Null(saved.ColorModeOverride);
        Assert.Null(saved.CompressionOverride);
    }

    [Fact]
    public async Task ColorModeOverrideIle_IsOlusturulunca_OverrideSaklanmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var repo = _sp.GetRequiredService<IJobRepository>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        await using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var job = await createHandler.HandleAsync(
            "test.pdf", stream, ConversionProfileKind.OcrGray300Lzw,
            colorModeOverride: ColorMode.Binary,
            compressionOverride: CompressionType.LZW);

        await queue.DequeueAsync();

        var saved = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(saved);
        Assert.Equal(ColorMode.Binary, saved!.ColorModeOverride);
        Assert.Equal(CompressionType.LZW, saved.CompressionOverride);
    }

    [Fact]
    public async Task DesteklenmeFormatIle_IsIslenince_FailedOlmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = _sp.GetRequiredService<ProcessConversionJobHandler>();
        var statusHandler = _sp.GetRequiredService<GetConversionJobStatusHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        await using var stream = new MemoryStream(new byte[] { 0x00 });
        var job = await createHandler.HandleAsync(
            "bilinmeyen.xyz", stream, ConversionProfileKind.OcrBinary300G4);
        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.UnsupportedFormat, result.FailureCategory);

        var finalJob = await statusHandler.HandleAsync(jobId);
        Assert.Equal(JobStatus.Failed, finalJob!.Status);
        Assert.NotNull(finalJob.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, recursive: true);
    }
}