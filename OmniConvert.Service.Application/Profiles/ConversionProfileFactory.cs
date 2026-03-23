namespace OmniConvert.Service.Application.Profiles;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// ConversionProfileKind → ConversionProfile eşlemesini merkezi olarak yönetir.
/// Yeni profil eklemek için sadece bu sözlüğü genişletmek yeterlidir.
/// </summary>
public class ConversionProfileFactory
{
    private static readonly IReadOnlyDictionary<ConversionProfileKind, ConversionProfile> Profiles =
        new Dictionary<ConversionProfileKind, ConversionProfile>
        {
            [ConversionProfileKind.OcrGray300Lzw] = new(
                ConversionProfileKind.OcrGray300Lzw, Dpi: 300, ColorMode: "Gray", CompressionType: "LZW"),

            [ConversionProfileKind.OcrBinary300G4] = new(
                ConversionProfileKind.OcrBinary300G4, Dpi: 300, ColorMode: "Binary", CompressionType: "G4"),

            [ConversionProfileKind.ArchiveColor300Lzw] = new(
                ConversionProfileKind.ArchiveColor300Lzw, Dpi: 300, ColorMode: "Color", CompressionType: "LZW"),
        };

    public ConversionProfile GetProfile(ConversionProfileKind kind)
    {
        if (Profiles.TryGetValue(kind, out var profile))
            return profile;

        throw new ArgumentOutOfRangeException(nameof(kind), $"Tanımsız profil türü: {kind}");
    }
}