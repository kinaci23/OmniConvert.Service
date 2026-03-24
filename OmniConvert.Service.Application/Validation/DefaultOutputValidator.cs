namespace OmniConvert.Service.Application.Validation;

using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Minimum çıktı doğrulaması:
/// 1. Path boş değil
/// 2. Uzantı .tif veya .tiff
/// 3. Dosya gerçekten var
/// İleride: DPI, frame sayısı, compression tipi kontrolleri eklenecek.
/// </summary>
public class DefaultOutputValidator : IOutputValidator
{
    public Task<bool> ValidateAsync(
        OutputValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.OutputFilePath))
            return Task.FromResult(false);

        var ext = Path.GetExtension(context.OutputFilePath)
                      ?.TrimStart('.')
                      .ToLowerInvariant();

        if (ext is not ("tif" or "tiff"))
            return Task.FromResult(false);

        return Task.FromResult(File.Exists(context.OutputFilePath));
    }
}