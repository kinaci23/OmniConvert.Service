using OmniConvert.Service.Api.DependencyInjection;
using OmniConvert.Service.Worker.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "OmniConvert API", Version = "v1" });
});

builder.Services.AddApiServices(builder.Configuration);

// Worker ayný process içinde çalýþýr; ayný in-memory repo ve queue'yu paylaþýr
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