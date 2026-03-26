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
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;
using OmniConvert.Service.Infrastructure.Concurrency;
using OmniConvert.Service.Infrastructure.Configuration;
using OmniConvert.Service.Infrastructure.Persistence;
using OmniConvert.Service.Infrastructure.Queue;
using OmniConvert.Service.Infrastructure.Storage;
using OmniConvert.Service.Infrastructure.Temp;
using OmniConvert.Service.Infrastructure.Time;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using Xunit;

public class XlsxFallbackFlowTests : IDisposable
{
    private readonly string _testBasePath;

    public XlsxFallbackFlowTests()
    {
        _testBasePath = Path.Combine(
            Path.GetTempPath(), "OmniConvertXlsxFallbackTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);
    }

    [Fact]
    public async Task XlsxJob_SyncfusionBasarisiz_FallbackLibreOfficeKullanmali()
    {
        var sp = BuildServiceProvider(syncfusionFails: true, libreOfficeFails: false);

        var createHandler = sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = sp.GetRequiredService<ProcessConversionJobHandler>();
        var statusHandler = sp.GetRequiredService<GetConversionJobStatusHandler>();
        var queue = sp.GetRequiredService<IJobQueue>();

        var inputPath = CreateTestXlsx("test_fallback.xlsx");
        await using var stream = File.OpenRead(inputPath);

        var job = await createHandler.HandleAsync(
            "test_fallback.xlsx",
            stream,
            ConversionProfileKind.ArchiveColor300Lzw);

        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.True(result.Success, result.ErrorMessage ?? "Fallback başarısız oldu");
        Assert.True(result.UsedFallback, "Fallback kullanılmadı");

        var finalJob = await statusHandler.HandleAsync(jobId);
        Assert.Equal(JobStatus.CompletedWithFallback, finalJob!.Status);
        Assert.True(File.Exists(finalJob.OutputPath));
    }

    [Fact]
    public async Task XlsxJob_HerIkiPipelineBasarisiz_FailedOlmali()
    {
        var sp = BuildServiceProvider(syncfusionFails: true, libreOfficeFails: true);

        var createHandler = sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = sp.GetRequiredService<ProcessConversionJobHandler>();
        var statusHandler = sp.GetRequiredService<GetConversionJobStatusHandler>();
        var queue = sp.GetRequiredService<IJobQueue>();

        var inputPath = CreateTestXlsx("test_both_fail.xlsx");
        await using var stream = File.OpenRead(inputPath);

        var job = await createHandler.HandleAsync(
            "test_both_fail.xlsx",
            stream,
            ConversionProfileKind.ArchiveColor300Lzw);

        var jobId = await queue.DequeueAsync();
        var result = await processHandler.HandleAsync(jobId);

        Assert.False(result.Success);

        var finalJob = await statusHandler.HandleAsync(jobId);
        Assert.Equal(JobStatus.Failed, finalJob!.Status);
    }

    [Fact]
    public async Task JobIslendikten_SonraTempWorkspace_TemizlenmisMali()
    {
        var sp = BuildServiceProvider(syncfusionFails: false, libreOfficeFails: false);

        var createHandler = sp.GetRequiredService<CreateConversionJobHandler>();
        var processHandler = sp.GetRequiredService<ProcessConversionJobHandler>();
        var queue = sp.GetRequiredService<IJobQueue>();

        var inputPath = CreateTestXlsx("test_cleanup.xlsx");
        await using var stream = File.OpenRead(inputPath);

        var job = await createHandler.HandleAsync(
            "test_cleanup.xlsx",
            stream,
            ConversionProfileKind.ArchiveColor300Lzw);

        var jobId = await queue.DequeueAsync();
        await processHandler.HandleAsync(jobId);

        var expectedWorkspace = Path.Combine(
            Path.GetTempPath(), "OmniConvert", "workspaces", jobId.ToString());

        Assert.False(Directory.Exists(expectedWorkspace),
            "Temp workspace job bittikten sonra temizlenmemiş!");
    }

    // -------------------------------------------------------------------------

    private IServiceProvider BuildServiceProvider(
        bool syncfusionFails,
        bool libreOfficeFails)
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.Configure<StorageOptions>(o => o.BasePath = _testBasePath);
        services.Configure<ConcurrencyOptions>(o =>
        {
            o.SyncfusionExcelRenderMerge = 1;
            o.LibreOfficeExcelPdfBridge = 1;
            o.TotalMaxConcurrent = 4;
        });

        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();
        services.AddSingleton<IConcurrencyLimiter, PipelineConcurrencyLimiter>();

        services.AddTransient<IStorageService, LocalFileStorageService>();
        services.AddTransient<ITempWorkspaceFactory, TempWorkspaceFactory>();
        services.AddTransient<IClock, SystemClock>();

        if (syncfusionFails)
            services.AddTransient<IConversionPipeline>(
                _ => new FakeFailingPipeline(
                    PipelineKind.SyncfusionExcelRenderMerge, SourceFormat.Xlsx));
        else
            services.AddTransient<IConversionPipeline>(
                _ => new FakeSucceedingPipeline(
                    PipelineKind.SyncfusionExcelRenderMerge, SourceFormat.Xlsx));

        if (libreOfficeFails)
            services.AddTransient<IConversionPipeline>(
                _ => new FakeFailingPipeline(
                    PipelineKind.LibreOfficeExcelPdfBridge, SourceFormat.Xlsx));
        else
            services.AddTransient<IConversionPipeline>(
                _ => new FakeSucceedingPipeline(
                    PipelineKind.LibreOfficeExcelPdfBridge, SourceFormat.Xlsx));

        services.AddTransient<IPipelineSelector, DefaultPipelineSelector>();
        services.AddTransient<IOutputValidator, DefaultOutputValidator>();
        services.AddTransient<ConversionProfileResolver>();
        services.AddSingleton<ILogger<ConversionOrchestrator>>(
            NullLogger<ConversionOrchestrator>.Instance);
        services.AddTransient<IConversionOrchestrator, ConversionOrchestrator>();

        services.AddTransient<CreateConversionJobHandler>();
        services.AddTransient<ProcessConversionJobHandler>();
        services.AddTransient<GetConversionJobStatusHandler>();

        return services.BuildServiceProvider();
    }

    private string CreateTestXlsx(string fileName)
    {
        var path = Path.Combine(_testBasePath, fileName);

        using var engine = new ExcelEngine();
        IApplication app = engine.Excel;
        app.DefaultVersion = ExcelVersion.Xlsx;
        app.XlsIORenderer = new XlsIORenderer();
        IWorkbook workbook = app.Workbooks.Create(1);
        workbook.Worksheets[0].Range["A1"].Value = "OmniConvert Fallback Test";

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        workbook.SaveAs(fs);

        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, recursive: true);
    }

    // -------------------------------------------------------------------------

    private sealed class FakeFailingPipeline : IConversionPipeline
    {
        private readonly PipelineKind _kind;
        private readonly SourceFormat _format;

        public FakeFailingPipeline(PipelineKind kind, SourceFormat format)
        {
            _kind = kind;
            _format = format;
        }

        public PipelineKind Kind => _kind;

        public bool CanHandle(SourceFormat format) => format == _format;

        public Task<PipelineExecutionResult> ExecuteAsync(
            ConversionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"Fake failure: {_kind}",
                FailureCategory: FailureCategory.ExternalProcess));
    }

    private sealed class FakeSucceedingPipeline : IConversionPipeline
    {
        private readonly PipelineKind _kind;
        private readonly SourceFormat _format;

        public FakeSucceedingPipeline(PipelineKind kind, SourceFormat format)
        {
            _kind = kind;
            _format = format;
        }

        public PipelineKind Kind => _kind;

        public bool CanHandle(SourceFormat format) => format == _format;

        public Task<PipelineExecutionResult> ExecuteAsync(
            ConversionContext context,
            CancellationToken cancellationToken = default)
        {
            var dir = Path.GetDirectoryName(context.OutputFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var image = new MagickImage(MagickColors.White, 16, 16);
            image.Format = MagickFormat.Tiff;
            image.Settings.Compression = CompressionMethod.LZW;
            image.Write(context.OutputFilePath);

            return Task.FromResult(new PipelineExecutionResult(
                Success: true,
                OutputPath: context.OutputFilePath));
        }
    }
}