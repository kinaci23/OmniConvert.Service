namespace OmniConvert.Service.Core.ValueObjects;

/// <summary>Doğrulama sonucu ve açıklayıcı mesajı taşır.</summary>
public record ValidationResult(bool IsValid, string? Message = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Fail(string message) => new(false, message);
}