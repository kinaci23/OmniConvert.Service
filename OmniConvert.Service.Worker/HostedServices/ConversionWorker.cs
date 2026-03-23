namespace OmniConvert.Service.Worker.HostedServices;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Core.Interfaces;

/// <summary>
/// Kuyruktan iş ID'si alır ve ProcessConversionJobHandler'a iletir.
/// CancellationToken iptal edildiğinde temiz şekilde durur.
/// </summary>
public class ConversionWorker : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly ProcessConversionJobHandler _handler;
    private readonly ILogger<ConversionWorker> _logger;

    public ConversionWorker(
        IJobQueue jobQueue,
        ProcessConversionJobHandler handler,
        ILogger<ConversionWorker> logger)
    {
        _jobQueue = jobQueue;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConversionWorker başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _jobQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("İş alındı: {JobId}", jobId);

                var result = await _handler.HandleAsync(jobId, stoppingToken);

                if (result.Success)
                    _logger.LogInformation("İş tamamlandı: {JobId} | Fallback: {UsedFallback}",
                        jobId, result.UsedFallback);
                else
                    _logger.LogWarning("İş başarısız: {JobId} | Hata: {Error}",
                        jobId, result.ErrorMessage);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConversionWorker içinde işlenmeyen hata.");
            }
        }

        _logger.LogInformation("ConversionWorker durduruldu.");
    }
}