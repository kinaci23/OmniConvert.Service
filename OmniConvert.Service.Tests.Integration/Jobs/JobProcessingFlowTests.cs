namespace OmniConvert.Service.Tests.Integration.Jobs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Application.Orchestration;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Application.Selection;
using OmniConvert.Service.Application.Validation;
using OmniConvert.Service.Conversion.Pipelines.Excel;
using OmniConvert.Service.Conversion.Pipelines.Pdf;
using OmniConvert.Service.Conversion.Pipelines.Raster;
using OmniConvert.Service.Conversion.Pipelines.Word;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Infrastructure.Configuration;
using OmniConvert.Service.Infrastructure.Persistence;
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

        services.Configure<StorageOptions>(o => o.BasePath = _testBasePath);

        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();

        services.AddTransient<IStorageService, LocalFileStorageService>();
        services.AddTransient<ITempWorkspaceFactory, TempWorkspaceFactory>();
        services.AddTransient<IClock, SystemClock>();

        services.AddTransient<IConversionPipeline, LibreOfficeWordPdfBridgePipeline>();
        services.AddTransient<IConversionPipeline, SyncfusionExcelRenderMergePipeline>();
        services.AddTransient<IConversionPipeline, LibreOfficeExcelPdfBridgePipeline>();
        services.AddTransient<IConversionPipeline, GhostscriptScaledPipeline>();
        services.AddTransient<IConversionPipeline, RasterMagickPipeline>();

        services.AddTransient<IPipelineSelector, DefaultPipelineSelector>();
        services.AddTransient<IOutputValidator, DefaultOutputValidator>();
        services.AddTransient<ConversionProfileFactory>();
        services.AddSingleton<ILogger<ConversionOrchestrator>>(
            NullLogger<ConversionOrchestrator>.Instance);
        services.AddTransient<IConversionOrchestrator, ConversionOrchestrator>();

        services.AddTransient<CreateConversionJobHandler>();
        services.AddTransient<ProcessConversionJobHandler>();
        services.AddTransient<GetConversionJobStatusHandler>();

        _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PdfIsi_OlusturulupIslenince_StatusCompleted_Olmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = _sp.GetRequiredService<ProcessConversionJobHandler>();
        var statusHandler = _sp.GetRequiredService<GetConversionJobStatusHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        var job = await createHandler.HandleAsync(
            "rapor.pdf", ConversionProfileKind.ArchiveColor300Lzw);

        Assert.Equal(JobStatus.Queued, job.Status);

        var jobId = await queue.DequeueAsync();
        Assert.Equal(job.Id, jobId);

        var result = await processHandler.HandleAsync(jobId);

        Assert.True(result.Success);
        Assert.Equal(PipelineKind.GhostscriptScaled, result.PipelineUsed);
        Assert.False(result.UsedFallback);
        Assert.Null(result.FailureCategory);

        var finalJob = await statusHandler.HandleAsync(jobId);
        Assert.NotNull(finalJob);
        Assert.Equal(JobStatus.Completed, finalJob!.Status);
        Assert.NotNull(finalJob.OutputPath);
        Assert.True(File.Exists(finalJob.OutputPath));
    }

    [Fact]
    public async Task DocxIsi_IslenirkenLibreOfficePipeline_Kullanilmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = _sp.GetRequiredService<ProcessConversionJobHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        var job = await createHandler.HandleAsync("belge.docx", ConversionProfileKind.OcrGray300Lzw);
        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.True(result.Success);
        Assert.Equal(PipelineKind.LibreOfficeWordPdfBridge, result.PipelineUsed);
    }

    [Fact]
    public async Task DesteklenmeFormatIle_IsIslenince_StatusFailed_Olmali()
    {
        var createHandler = _sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = _sp.GetRequiredService<ProcessConversionJobHandler>();
        var statusHandler = _sp.GetRequiredService<GetConversionJobStatusHandler>();
        var queue = _sp.GetRequiredService<IJobQueue>();

        // .xyz uzantısı → SourceFormat.Unknown → selector NotSupportedException fırlatır
        var job = await createHandler.HandleAsync(
            "bilinmeyen.xyz", ConversionProfileKind.OcrBinary300G4);

        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.False(result.Success);
        Assert.Equal(FailureCategory.UnsupportedFormat, result.FailureCategory);

        var finalJob = await statusHandler.HandleAsync(jobId);
        Assert.NotNull(finalJob);
        Assert.Equal(JobStatus.Failed, finalJob!.Status);
        Assert.NotNull(finalJob.ErrorMessage);
    }

    public void Dispose()
    {
        // Test sonrası geçici klasörü temizle
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, recursive: true);
    }
}