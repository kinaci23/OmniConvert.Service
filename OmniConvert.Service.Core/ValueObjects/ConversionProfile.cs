namespace OmniConvert.Service.Core.ValueObjects;

using OmniConvert.Service.Core.Enums;

/// <summary>
/// Bir iş için çözümlenmiş final dönüşüm profili.
/// Preset + kullanıcı override'larından oluşturulur.
/// </summary>
public record ConversionProfile(
    ConversionProfileKind Kind,
    int Dpi,
    ColorMode ColorMode,
    CompressionType CompressionType,
    bool IsCustomized = false,
    int? JpegQuality = null,
    byte? Threshold = null
);