using System.Text.Json.Serialization;
using OmniConvert.Service.Api.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// ── Windows Service desteği ───────────────────────────────────────────────────
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OmniConvert.Service";
});

// ── Windows Service modunda çalışma dizini güvenliği ─────────────────────────
if (!builder.Environment.IsDevelopment())
{
    var exeDir = AppContext.BaseDirectory;
    builder.Host.UseContentRoot(exeDir);
    Directory.SetCurrentDirectory(exeDir);
}

// ── Kestrel ──────────────────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104_857_600;
});

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

#pragma warning disable CA1416
if (OperatingSystem.IsWindows() && !builder.Environment.IsDevelopment())
{
    builder.Logging.AddEventLog(settings =>
    {
        settings.SourceName = "OmniConvert.Service";
    });
}
#pragma warning restore CA1416

// ── Servisler ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "OmniConvert API", Version = "v1" });
    options.UseInlineDefinitionsForEnums();
});

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddHostedService<ConversionWorker>();

// ── Uygulama ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();