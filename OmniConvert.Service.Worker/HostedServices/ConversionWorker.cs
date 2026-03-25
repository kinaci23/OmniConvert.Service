namespace OmniConvert.Service.Worker.HostedServices;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniConvert.Service.Application.Jobs;
using OmniConvert.Service.Core.Interfaces;

/// <summary>
/// Kuyruktan iş alır ve orchestrator'a iletir.
/// Graceful shutdown: stoppingToken iptal edildiğinde mevcut işi tamamlar, yeni iş almaz.
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
        _logger.LogInformation(
            "ConversionWorker başlatıldı | ThreadId: {ThreadId}",
            Environment.CurrentManagedThreadId);

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId = default;
            try
            {
                // Kuyrukta iş yoksa bekler — CPU tüketmez
                jobId = await _jobQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("İş alındı | JobId: {JobId}", jobId);

                var result = await _handler.HandleAsync(jobId, stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Worker: İş tamamlandı | JobId: {JobId} | Pipeline: {Pipeline} | Fallback: {Fallback}",
                        jobId, result.PipelineUsed, result.UsedFallback);
                }
                else
                {
                    _logger.LogWarning(
                        "Worker: İş başarısız | JobId: {JobId} | Category: {Category} | Error: {Error}",
                        jobId, result.FailureCategory, result.ErrorMessage);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — döngüden temiz çık
                _logger.LogInformation(
                    "ConversionWorker durduruluyor — iptal sinyali alındı.");
                break;
            }
            catch (Exception ex)
            {
                // Orchestrator job'u Failed olarak işaretledi — worker devam eder
                _logger.LogError(ex,
                    "Worker: İşlenmeyen hata | JobId: {JobId}", jobId);
            }
        }

        _logger.LogInformation("ConversionWorker durduruldu.");
    }
}