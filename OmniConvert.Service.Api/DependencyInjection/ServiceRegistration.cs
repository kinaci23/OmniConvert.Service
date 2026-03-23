namespace OmniConvert.Service.Api.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Application.Orchestration;
using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Application.Selection;
using OmniConvert.Service.Application.Validation;
using OmniConvert.Service.Conversion.Pipelines.Excel;
using OmniConvert.Service.Conversion.Pipelines.Pdf;
using OmniConvert.Service.Conversion.Pipelines.Raster;
using OmniConvert.Service.Conversion.Pipelines.Word;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Infrastructure.Configuration;
using OmniConvert.Service.Infrastructure.Persistence;
using OmniConvert.Service.Infrastructure.Processes;
using OmniConvert.Service.Infrastructure.Queue;
using OmniConvert.Service.Infrastructure.Storage;
using OmniConvert.Service.Infrastructure.Temp;
using OmniConvert.Service.Infrastructure.Time;

public static class ServiceRegistration
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();

        services.AddTransient<IStorageService, LocalFileStorageService>();
        services.AddTransient<ITempWorkspaceFactory, TempWorkspaceFactory>();
        services.AddTransient<IExternalProcessRunner, ExternalProcessRunner>();
        services.AddTransient<IClock, SystemClock>();

        services.AddTransient<IConversionPipeline, LibreOfficeWordPdfBridgePipeline>();
        services.AddTransient<IConversionPipeline, SyncfusionExcelRenderMergePipeline>();
        services.AddTransient<IConversionPipeline, LibreOfficeExcelPdfBridgePipeline>();
        services.AddTransient<IConversionPipeline, GhostscriptScaledPipeline>();
        services.AddTransient<IConversionPipeline, RasterMagickPipeline>();

        services.AddTransient<IPipelineSelector, DefaultPipelineSelector>();
        services.AddTransient<IOutputValidator, DefaultOutputValidator>();
        services.AddTransient<ConversionProfileFactory>();
        services.AddTransient<IConversionOrchestrator, ConversionOrchestrator>();

        services.AddTransient<CreateConversionJobHandler>();
        services.AddTransient<ProcessConversionJobHandler>();
        services.AddTransient<GetConversionJobStatusHandler>();

        return services;
    }
}