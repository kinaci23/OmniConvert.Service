using System.Text.Json.Serialization;
using OmniConvert.Service.Api.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Enum'lar JSON'da string olarak serileþtirilir/ayrýþtýrýlýr
// Örn: ColorMode.Gray ? "Gray", "Gray" ? ColorMode.Gray
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "OmniConvert API", Version = "v1" });
    options.UseInlineDefinitionsForEnums();
});

// Tüm servisler tek DI container'da — Worker ayný instance'larý paylaþýr
builder.Services.AddApiServices(builder.Configuration);

// Worker API host içinde BackgroundService olarak çalýþýr.
// In-memory queue ve repository singleton olduðundan paylaþým otomatiktir.
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