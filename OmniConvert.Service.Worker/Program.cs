using OmniConvert.Service.Worker.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerServices(builder.Configuration);
builder.Services.AddHostedService<ConversionWorker>();

var host = builder.Build();
await host.RunAsync();