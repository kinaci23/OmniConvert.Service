namespace OmniConvert.Service.Application.Validation;

using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Minimum doğrulama: path boş değil + dosya gerçekten var mı.
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

        var exists = File.Exists(context.OutputFilePath);
        return Task.FromResult(exists);
    }
}