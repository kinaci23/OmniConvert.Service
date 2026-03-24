namespace OmniConvert.Service.Worker.HostedServices;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Core.Interfaces;

/// <summary>
/// Kuyruktan iş alır ve orchestrator'a iletir.
/// Graceful shutdown: mevcut iş tamamlanır, yeni iş alınmaz.
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
            Guid jobId = default;

            try
            {
                // Kuyrukta iş yoksa burada bekler — CPU tüketmez
                jobId = await _jobQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("İş işleniyor: {JobId}", jobId);

                // stoppingToken geçiliyor: iptal sinyali alınırsa pipeline da durur
                var result = await _handler.HandleAsync(jobId, stoppingToken);

                if (result.Success)
                    _logger.LogInformation(
                        "İş tamamlandı: {JobId} | Pipeline: {Pipeline} | Fallback: {Fallback}",
                        jobId, result.PipelineUsed, result.UsedFallback);
                else
                    _logger.LogWarning(
                        "İş başarısız: {JobId} | Kategori: {Category} | Hata: {Error}",
                        jobId, result.FailureCategory, result.ErrorMessage);
            }
            catch (OperationCanceledException)
            {
                // Shutdown sinyali — döngüden temiz çık
                _logger.LogInformation(
                    "ConversionWorker durduruluyor — iptal sinyali alındı.");
                break;
            }
            catch (Exception ex)
            {
                // İşlenmeyen hata — job zaten Failed olarak işaretlendi (Orchestrator garantisi)
                // Worker çalışmaya devam eder
                _logger.LogError(ex,
                    "ConversionWorker içinde işlenmeyen hata. Job: {JobId}", jobId);
            }
        }

        _logger.LogInformation("ConversionWorker durduruldu.");
    }
}