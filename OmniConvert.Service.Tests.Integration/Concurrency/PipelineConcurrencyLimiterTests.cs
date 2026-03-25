namespace OmniConvert.Service.Tests.Integration.Concurrency;

using Microsoft.Extensions.Options;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Infrastructure.Concurrency;
using OmniConvert.Service.Infrastructure.Configuration;
using Xunit;

public class PipelineConcurrencyLimiterTests
{
    [Fact]
    public async Task Acquire_AndRelease_SlotuDuzgunBirakir()
    {
        var options = Options.Create(new ConcurrencyOptions
        {
            GhostscriptScaled = 1,
            TotalMaxConcurrent = 2
        });
        using var limiter = new PipelineConcurrencyLimiter(options);

        using (var slot = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled))
            Assert.NotNull(slot);

        // Slot bırakıldı — yeniden alınabilmeli
        using var slot2 = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled);
        Assert.NotNull(slot2);
    }

    [Fact]
    public async Task Acquire_LimitAsilincaBloklar()
    {
        var options = Options.Create(new ConcurrencyOptions
        {
            GhostscriptScaled = 1,
            TotalMaxConcurrent = 2
        });
        using var limiter = new PipelineConcurrencyLimiter(options);

        // İlk slot alınır, bırakılmaz
        var slot1 = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled);

        // Limit aşıldı — iptal edilmeli
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            limiter.AcquireAsync(PipelineKind.GhostscriptScaled, cts.Token));

        slot1.Dispose();
    }

    [Fact]
    public async Task FarklıPipelinelar_BagimsizCalisir()
    {
        var options = Options.Create(new ConcurrencyOptions
        {
            GhostscriptScaled = 1,
            RasterMagick = 1,
            TotalMaxConcurrent = 4
        });
        using var limiter = new PipelineConcurrencyLimiter(options);

        // Ghostscript dolu
        var gsSlot = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled);

        // RasterMagick bağımsız olarak alınabilmeli
        using var rmSlot = await limiter.AcquireAsync(PipelineKind.RasterMagick);
        Assert.NotNull(rmSlot);

        gsSlot.Dispose();
    }

    [Fact]
    public async Task TotalLimit_AsilirsaFarklıPipelineDaBloklar()
    {
        var options = Options.Create(new ConcurrencyOptions
        {
            GhostscriptScaled = 5,
            RasterMagick = 5,
            TotalMaxConcurrent = 1   // Toplam limit = 1
        });
        using var limiter = new PipelineConcurrencyLimiter(options);

        var slot1 = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled);

        // Toplam doldu — farklı pipeline da olsa bloke olmalı
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            limiter.AcquireAsync(PipelineKind.RasterMagick, cts.Token));

        slot1.Dispose();
    }

    [Fact]
    public async Task Dispose_IdempotentDir()
    {
        var options = Options.Create(new ConcurrencyOptions
        {
            GhostscriptScaled = 1,
            TotalMaxConcurrent = 2
        });
        using var limiter = new PipelineConcurrencyLimiter(options);

        var slot = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled);

        // İki kez dispose — exception fırlatmamalı
        slot.Dispose();
        slot.Dispose();

        // Slot bırakıldı, yeniden alınabilmeli
        using var slot2 = await limiter.AcquireAsync(PipelineKind.GhostscriptScaled);
        Assert.NotNull(slot2);
    }
}