using OmniConvert.Service.Api.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "OmniConvert API", Version = "v1" });
});

// Tüm servisler burada kayýtlý — Worker da ayný DI container'ý kullanýr
builder.Services.AddApiServices(builder.Configuration);

// Worker, API host içinde BackgroundService olarak çalýþýr.
// In-memory queue ve repository ayný singleton instance'lar üzerinden paylaþýlýr.
builder.Services.AddHostedService<ConversionWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();