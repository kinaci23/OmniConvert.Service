// Standalone worker modu — geliþtirme aþamasýnda kullanýlmaz.
// Geliþtirme ortamýnda OmniConvert.Service.Api ana entry point'tir;
// Worker orada BackgroundService olarak kayýtlýdýr.
// Bu program, ileride Worker'ýn ayrý bir host veya Windows Service olarak
// çalýþtýrýlmasý gerektiðinde aktif hale getirilecektir.

using OmniConvert.Service.Worker.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerServices(builder.Configuration);
builder.Services.AddHostedService<ConversionWorker>();

var host = builder.Build();
await host.RunAsync();