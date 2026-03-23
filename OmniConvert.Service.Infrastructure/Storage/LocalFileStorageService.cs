namespace OmniConvert.Service.Infrastructure.Storage;

using Microsoft.Extensions.Options;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;
using OmniConvert.Service.Infrastructure.Configuration;

public class LocalFileStorageService : IStorageService
{
    private readonly StorageOptions _options;

    public LocalFileStorageService(IOptions<StorageOptions> options)
        => _options = options.Value;

    public Task<StoragePathSet> PrepareJobStorageAsync(
        Guid jobId,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var jobDir = Path.Combine(_options.BasePath, jobId.ToString());
        var inputDir = Path.Combine(jobDir, "input");
        var outputDir = Path.Combine(jobDir, "output");
        var workspaceDir = Path.Combine(jobDir, "workspace");

        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(workspaceDir);

        var inputPath = Path.Combine(inputDir, originalFileName);
        var outputPath = Path.Combine(outputDir,
            $"{Path.GetFileNameWithoutExtension(originalFileName)}.tif");

        return Task.FromResult(new StoragePathSet(inputPath, outputPath, workspaceDir));
    }
}