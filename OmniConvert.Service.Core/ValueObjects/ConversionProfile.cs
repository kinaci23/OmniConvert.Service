namespace OmniConvert.Service.Core.ValueObjects;

using OmniConvert.Service.Core.Enums;

/// <summary>Dönüşüm profili parametrelerini taşıyan değer nesnesi.</summary>
public record ConversionProfile(
    ConversionProfileKind Kind,
    int Dpi,
    string ColorMode,
    string CompressionType
);