namespace OmniConvert.Service.Core.ValueObjects;

using OmniConvert.Service.Core.Enums;

/// <summary>
/// Bir iş için çözümlenmiş (resolved) final dönüşüm profili.
/// Preset + kullanıcı override'larından oluşturulur.
/// </summary>
public record ConversionProfile(
    ConversionProfileKind Kind,
    int Dpi,
    string ColorMode,
    string CompressionType,
    bool IsCustomized = false
);