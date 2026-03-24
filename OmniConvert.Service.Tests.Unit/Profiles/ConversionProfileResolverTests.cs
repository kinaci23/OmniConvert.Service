namespace OmniConvert.Service.Tests.Unit.Profiles;

using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Core.Enums;
using Xunit;

public class ConversionProfileResolverTests
{
    private readonly ConversionProfileResolver _resolver = new();

    // --- Preset testleri ---

    [Fact]
    public void OcrGray300Lzw_PresetDegerleriniDogruDonmeli()
    {
        var profile = _resolver.Resolve(ConversionProfileKind.OcrGray300Lzw);

        Assert.Equal(300, profile.Dpi);
        Assert.Equal("Gray", profile.ColorMode);
        Assert.Equal("LZW", profile.CompressionType);
        Assert.False(profile.IsCustomized);
    }

    [Fact]
    public void OcrBinary300G4_PresetDegerleriniDogruDonmeli()
    {
        var profile = _resolver.Resolve(ConversionProfileKind.OcrBinary300G4);

        Assert.Equal(300, profile.Dpi);
        Assert.Equal("Binary", profile.ColorMode);
        Assert.Equal("G4", profile.CompressionType);
        Assert.False(profile.IsCustomized);
    }

    [Fact]
    public void ArchiveColor300Lzw_PresetDegerleriniDogruDonmeli()
    {
        var profile = _resolver.Resolve(ConversionProfileKind.ArchiveColor300Lzw);

        Assert.Equal(300, profile.Dpi);
        Assert.Equal("Color", profile.ColorMode);
        Assert.Equal("LZW", profile.CompressionType);
        Assert.False(profile.IsCustomized);
    }

    // --- Override testleri ---

    [Fact]
    public void DpiOverride_UygulandığındaIsCustomizedTrue_Olmali()
    {
        var profile = _resolver.Resolve(
            ConversionProfileKind.OcrGray300Lzw, dpiOverride: 600);

        Assert.Equal(600, profile.Dpi);
        Assert.True(profile.IsCustomized);
    }

    [Fact]
    public void KismiOverride_SadeceDegistirilenenAlanEtkilenmeli()
    {
        var profile = _resolver.Resolve(
            ConversionProfileKind.OcrGray300Lzw, dpiOverride: 400);

        Assert.Equal(400, profile.Dpi);
        Assert.Equal("Gray", profile.ColorMode);   // preset'ten
        Assert.Equal("LZW", profile.CompressionType); // preset'ten
    }

    // --- Validasyon testleri ---

    [Fact]
    public void ColorModG4Kombinasyonu_ArgumentException_Firlatmali()
    {
        // Color + G4 → geçersiz (G4 yalnızca Binary ile)
        Assert.Throws<ArgumentException>(() =>
            _resolver.Resolve(
                ConversionProfileKind.ArchiveColor300Lzw,
                colorModeOverride: "Color",
                compressionOverride: "G4"));
    }

    [Fact]
    public void GrayG4Kombinasyonu_ArgumentException_Firlatmali()
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.Resolve(
                ConversionProfileKind.OcrGray300Lzw,
                compressionOverride: "G4"));
    }

    [Fact]
    public void DesteklenmevenDpi_ArgumentException_Firlatmali()
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.Resolve(
                ConversionProfileKind.OcrGray300Lzw, dpiOverride: 72));
    }

    [Fact]
    public void GecersizPresetKind_ArgumentOutOfRangeException_Firlatmali()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _resolver.Resolve((ConversionProfileKind)99));
    }

    [Fact]
    public void BinaryLzwKombinasyonu_Gecerli_Olmali()
    {
        var profile = _resolver.Resolve(
            ConversionProfileKind.OcrBinary300G4,
            colorModeOverride: "Binary",
            compressionOverride: "LZW");

        Assert.Equal("Binary", profile.ColorMode);
        Assert.Equal("LZW", profile.CompressionType);
    }
}