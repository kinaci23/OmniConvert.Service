namespace OmniConvert.Service.Infrastructure.Concurrency;

using Microsoft.Extensions.Options;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Infrastructure.Configuration;

/// <summary>
/// Her pipeline tipi için ayrı SemaphoreSlim + toplam slot yöneticisi.
/// Singleton olarak kayıtlı olmalı — SemaphoreSlim state içinde tutulur.
/// </summary>
public sealed class PipelineConcurrencyLimiter : IConcurrencyLimiter, IDisposable
{
    private readonly IReadOnlyDictionary<PipelineKind, SemaphoreSlim> _pipelineSemaphores;
    private readonly SemaphoreSlim _totalSemaphore;

    public PipelineConcurrencyLimiter(IOptions<ConcurrencyOptions> options)
    {
        var o = options.Value;

        _pipelineSemaphores = new Dictionary<PipelineKind, SemaphoreSlim>
        {
            [PipelineKind.GhostscriptScaled] = new(o.GhostscriptScaled, o.GhostscriptScaled),
            [PipelineKind.RasterMagick] = new(o.RasterMagick, o.RasterMagick),
            [PipelineKind.LibreOfficeWordPdfBridge] = new(o.LibreOfficeWordPdfBridge, o.LibreOfficeWordPdfBridge),
            [PipelineKind.SyncfusionExcelRenderMerge] = new(o.SyncfusionExcelRenderMerge, o.SyncfusionExcelRenderMerge),
            [PipelineKind.LibreOfficeExcelPdfBridge] = new(o.LibreOfficeExcelPdfBridge, o.LibreOfficeExcelPdfBridge),
        };

        _totalSemaphore = new SemaphoreSlim(o.TotalMaxConcurrent, o.TotalMaxConcurrent);
    }

    public async Task<IDisposable> AcquireAsync(
        PipelineKind pipeline,
        CancellationToken cancellationToken = default)
    {
        await _totalSemaphore.WaitAsync(cancellationToken);

        if (_pipelineSemaphores.TryGetValue(pipeline, out var pipelineSemaphore))
        {
            try
            {
                await pipelineSemaphore.WaitAsync(cancellationToken);
            }
            catch
            {
                _totalSemaphore.Release();
                throw;
            }

            return new SlotReleaser(_totalSemaphore, pipelineSemaphore);
        }

        return new SlotReleaser(_totalSemaphore, null);
    }

    public void Dispose()
    {
        foreach (var s in _pipelineSemaphores.Values)
            s.Dispose();
        _totalSemaphore.Dispose();
    }

    private sealed class SlotReleaser : IDisposable
    {
        private readonly SemaphoreSlim _total;
        private readonly SemaphoreSlim? _pipeline;
        private int _disposed;

        public SlotReleaser(SemaphoreSlim total, SemaphoreSlim? pipeline)
        {
            _total = total;
            _pipeline = pipeline;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _pipeline?.Release();
                _total.Release();
            }
        }
    }
}