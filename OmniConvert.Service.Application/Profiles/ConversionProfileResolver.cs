namespace OmniConvert.Service.Application.Profiles;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Preset + kullanıcı override'larından final ConversionProfile üretir.
/// Geçersiz kombinasyonları reddeder.
/// </summary>
public class ConversionProfileResolver
{
    private static readonly IReadOnlyDictionary<ConversionProfileKind, ConversionProfile> Presets =
        new Dictionary<ConversionProfileKind, ConversionProfile>
        {
            [ConversionProfileKind.OcrGray300Lzw] = new(
                ConversionProfileKind.OcrGray300Lzw,
                Dpi: 300, ColorMode: "Gray", CompressionType: "LZW"),

            [ConversionProfileKind.OcrBinary300G4] = new(
                ConversionProfileKind.OcrBinary300G4,
                Dpi: 300, ColorMode: "Binary", CompressionType: "G4"),

            [ConversionProfileKind.ArchiveColor300Lzw] = new(
                ConversionProfileKind.ArchiveColor300Lzw,
                Dpi: 300, ColorMode: "Color", CompressionType: "LZW"),
        };

    /// <summary>
    /// Geçerli ColorMode + Compression kombinasyonları.
    /// G4 yalnızca Binary ile kullanılabilir (faks standardı).
    /// </summary>
    private static readonly HashSet<(string ColorMode, string Compression)> ValidCombinations =
    [
        ("Binary", "G4"),
        ("Binary", "LZW"),
        ("Gray",   "LZW"),
        ("Color",  "LZW"),
    ];

    private static readonly int[] AllowedDpiValues = [150, 200, 300, 400, 600];

    /// <summary>
    /// Preset ve opsiyonel override'lardan final profil üretir.
    /// Geçersiz kombinasyon veya DPI durumunda ArgumentException fırlatır.
    /// </summary>
    public ConversionProfile Resolve(
        ConversionProfileKind presetKind,
        int? dpiOverride = null,
        string? colorModeOverride = null,
        string? compressionOverride = null)
    {
        if (!Presets.TryGetValue(presetKind, out var preset))
            throw new ArgumentOutOfRangeException(nameof(presetKind),
                $"Tanımsız preset: {presetKind}");

        var dpi = dpiOverride ?? preset.Dpi;
        var colorMode = colorModeOverride ?? preset.ColorMode;
        var compression = compressionOverride ?? preset.CompressionType;
        var isCustomized = dpiOverride.HasValue
                        || colorModeOverride != null
                        || compressionOverride != null;

        ValidateDpi(dpi);
        ValidateCombination(colorMode, compression);

        return new ConversionProfile(presetKind, dpi, colorMode, compression, isCustomized);
    }

    /// <summary>Preset değerini override uygulamadan döndürür.</summary>
    public ConversionProfile GetPreset(ConversionProfileKind kind)
    {
        if (!Presets.TryGetValue(kind, out var preset))
            throw new ArgumentOutOfRangeException(nameof(kind),
                $"Tanımsız preset: {kind}");
        return preset;
    }

    private static void ValidateDpi(int dpi)
    {
        if (!AllowedDpiValues.Contains(dpi))
            throw new ArgumentException(
                $"Desteklenmeyen DPI: {dpi}. " +
                $"İzin verilenler: {string.Join(", ", AllowedDpiValues)}",
                nameof(dpi));
    }

    private static void ValidateCombination(string colorMode, string compression)
    {
        if (!ValidCombinations.Contains((colorMode, compression)))
            throw new ArgumentException(
                $"Geçersiz kombinasyon: ColorMode={colorMode}, Compression={compression}. " +
                "G4 yalnızca Binary ile kullanılabilir; LZW tüm modlarla uyumludur.");
    }
}