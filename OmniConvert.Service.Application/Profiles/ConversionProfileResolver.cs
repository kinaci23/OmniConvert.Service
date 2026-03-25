namespace OmniConvert.Service.Application.Profiles;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Preset + kullanıcı override'larından final ConversionProfile üretir.
/// Type-safe enum'larla çalışır, geçersiz kombinasyonları reddeder.
/// </summary>
public class ConversionProfileResolver
{
    private static readonly IReadOnlyDictionary<ConversionProfileKind, ConversionProfile> Presets =
        new Dictionary<ConversionProfileKind, ConversionProfile>
        {
            [ConversionProfileKind.OcrGray300Lzw] = new(
                ConversionProfileKind.OcrGray300Lzw,
                Dpi: 300,
                ColorMode: ColorMode.Gray,
                CompressionType: CompressionType.LZW),

            [ConversionProfileKind.OcrBinary300G4] = new(
                ConversionProfileKind.OcrBinary300G4,
                Dpi: 300,
                ColorMode: ColorMode.Binary,
                CompressionType: CompressionType.G4),

            [ConversionProfileKind.ArchiveColor300Lzw] = new(
                ConversionProfileKind.ArchiveColor300Lzw,
                Dpi: 300,
                ColorMode: ColorMode.Color,
                CompressionType: CompressionType.LZW),
        };

    /// <summary>
    /// Geçerli ColorMode + CompressionType kombinasyonları.
    /// </summary>
    private static readonly HashSet<(ColorMode, CompressionType)> ValidCombinations =
    [
        (ColorMode.Binary, CompressionType.G4),
        (ColorMode.Binary, CompressionType.LZW),
        (ColorMode.Binary, CompressionType.None),
        (ColorMode.Gray,   CompressionType.LZW),
        (ColorMode.Gray,   CompressionType.None),
        (ColorMode.Color,  CompressionType.LZW),
        (ColorMode.Color,  CompressionType.Jpeg),
        (ColorMode.Color,  CompressionType.None),
    ];

    private static readonly int[] AllowedDpiValues = [150, 200, 300, 400, 600];

    public ConversionProfile Resolve(
        ConversionProfileKind presetKind,
        int? dpiOverride = null,
        ColorMode? colorModeOverride = null,
        CompressionType? compressionOverride = null,
        int? jpegQuality = null)
    {
        if (!Presets.TryGetValue(presetKind, out var preset))
            throw new ArgumentOutOfRangeException(nameof(presetKind),
                $"Tanımsız preset: {presetKind}");

        var dpi = dpiOverride ?? preset.Dpi;
        var colorMode = colorModeOverride ?? preset.ColorMode;
        var compression = compressionOverride ?? preset.CompressionType;
        var quality = jpegQuality ?? preset.JpegQuality;

        var isCustomized = dpiOverride.HasValue
                        || colorModeOverride.HasValue
                        || compressionOverride.HasValue
                        || jpegQuality.HasValue;

        ValidateDpi(dpi);
        ValidateCombination(colorMode, compression);

        return new ConversionProfile(presetKind, dpi, colorMode, compression, isCustomized, quality);
    }

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

    private static void ValidateCombination(ColorMode colorMode, CompressionType compression)
    {
        if (!ValidCombinations.Contains((colorMode, compression)))
            throw new ArgumentException(
                $"Geçersiz kombinasyon: ColorMode={colorMode}, Compression={compression}. " +
                "G4 yalnızca Binary ile, Jpeg yalnızca Color ile kullanılabilir.");
    }
}