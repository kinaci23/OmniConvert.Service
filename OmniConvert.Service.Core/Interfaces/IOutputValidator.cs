namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.ValueObjects;

public interface IOutputValidator
{
    Task<ValidationResult> ValidateAsync(
        OutputValidationContext context,
        CancellationToken cancellationToken = default);
}