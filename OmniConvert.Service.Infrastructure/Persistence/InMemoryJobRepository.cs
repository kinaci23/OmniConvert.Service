namespace OmniConvert.Service.Infrastructure.Persistence;

using System.Collections.Concurrent;
using OmniConvert.Service.Core.Entities;
using OmniConvert.Service.Core.Interfaces;

/// <summary>
/// Geliştirme amaçlı in-memory repository.
/// Uygulama yeniden başlatıldığında tüm veriler sıfırlanır.
/// Production'da SQL tabanlı implementasyonla değiştirilecektir.
/// </summary>
public class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, ConversionJob> _store = new();

    public Task<ConversionJob?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task SaveAsync(
        ConversionJob job,
        CancellationToken cancellationToken = default)
    {
        _store[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ConversionJob>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ConversionJob> list = [.. _store.Values];
        return Task.FromResult(list);
    }
}