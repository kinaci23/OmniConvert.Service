using System.Text.Json.Serialization;
using OmniConvert.Service.Api.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Multipart form-data için limit
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104_857_600; // 100 MB
});

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();