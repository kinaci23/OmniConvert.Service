namespace OmniConvert.Service.Application.Validation;

using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// v1 için minimal doğrulayıcı: çıktı yolunun boş olmadığını kontrol eder.
/// İleride gerçek TIFF doğrulaması buraya eklenir.
/// </summary>
public class DefaultOutputValidator : IOutputValidator
{
    public Task<bool> ValidateAsync(
        OutputValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var isValid = !string.IsNullOrWhiteSpace(context.OutputFilePath);
        return Task.FromResult(isValid);
    }
}