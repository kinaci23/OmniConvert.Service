namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.Entities;

public interface IJobRepository
{
    Task<ConversionJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(ConversionJob job, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversionJob>> GetAllAsync(CancellationToken cancellationToken = default);
}