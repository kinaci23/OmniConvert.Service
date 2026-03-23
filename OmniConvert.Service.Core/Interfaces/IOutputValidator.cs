namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.ValueObjects;

public interface IOutputValidator
{
    Task<bool> ValidateAsync(
        OutputValidationContext context,
        CancellationToken cancellationToken = default);
}