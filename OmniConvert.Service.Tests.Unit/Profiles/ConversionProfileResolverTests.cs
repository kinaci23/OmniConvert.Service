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
        Assert.Equal(ColorMode.Gray, profile.ColorMode);
        Assert.Equal(CompressionType.LZW, profile.CompressionType);
        Assert.False(profile.IsCustomized);
    }

    [Fact]
    public void OcrBinary300G4_PresetDegerleriniDogruDonmeli()
    {
        var profile = _resolver.Resolve(ConversionProfileKind.OcrBinary300G4);

        Assert.Equal(300, profile.Dpi);
        Assert.Equal(ColorMode.Binary, profile.ColorMode);
        Assert.Equal(CompressionType.G4, profile.CompressionType);
        Assert.False(profile.IsCustomized);
    }

    [Fact]
    public void ArchiveColor300Lzw_PresetDegerleriniDogruDonmeli()
    {
        var profile = _resolver.Resolve(ConversionProfileKind.ArchiveColor300Lzw);

        Assert.Equal(300, profile.Dpi);
        Assert.Equal(ColorMode.Color, profile.ColorMode);
        Assert.Equal(CompressionType.LZW, profile.CompressionType);
        Assert.False(profile.IsCustomized);
    }

    // --- Override testleri ---

    [Fact]
    public void DpiOverride_IsCustomizedTrue_Olmali()
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
        Assert.Equal(ColorMode.Gray, profile.ColorMode);
        Assert.Equal(CompressionType.LZW, profile.CompressionType);
    }

    [Fact]
    public void BinaryLzwKombinasyonu_Gecerli_Olmali()
    {
        var profile = _resolver.Resolve(
            ConversionProfileKind.OcrBinary300G4,
            colorModeOverride: ColorMode.Binary,
            compressionOverride: CompressionType.LZW);

        Assert.Equal(ColorMode.Binary, profile.ColorMode);
        Assert.Equal(CompressionType.LZW, profile.CompressionType);
        Assert.True(profile.IsCustomized);
    }

    // --- Validasyon testleri ---

    [Fact]
    public void ColorG4Kombinasyonu_ArgumentException_Firlatmali()
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.Resolve(
                ConversionProfileKind.ArchiveColor300Lzw,
                colorModeOverride: ColorMode.Color,
                compressionOverride: CompressionType.G4));
    }

    [Fact]
    public void GrayG4Kombinasyonu_ArgumentException_Firlatmali()
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.Resolve(
                ConversionProfileKind.OcrGray300Lzw,
                compressionOverride: CompressionType.G4));
    }

    [Fact]
    public void DesteklenmeyenDpi_ArgumentException_Firlatmali()
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.Resolve(
                ConversionProfileKind.OcrGray300Lzw,
                dpiOverride: 72));
    }

    [Fact]
    public void GecersizPresetKind_ArgumentOutOfRangeException_Firlatmali()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _resolver.Resolve((ConversionProfileKind)99));
    }
}