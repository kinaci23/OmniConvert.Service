namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.ValueObjects;

public interface IStorageService
{
    Task<StoragePathSet> PrepareJobStorageAsync(
        Guid jobId,
        string originalFileName,
        CancellationToken cancellationToken = default);
}