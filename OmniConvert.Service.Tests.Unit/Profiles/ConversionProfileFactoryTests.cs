namespace OmniConvert.Service.Tests.Unit.Profiles;

using OmniConvert.Service.Application.Profiles;
using OmniConvert.Service.Core.Enums;
using Xunit;

public class ConversionProfileFactoryTests
{
    private readonly ConversionProfileFactory _factory = new();

    [Fact]
    public void OcrGray300Lzw_DogruDegerlereDonmeli()
    {
        var profile = _factory.GetProfile(ConversionProfileKind.OcrGray300Lzw);

        Assert.Equal(ConversionProfileKind.OcrGray300Lzw, profile.Kind);
        Assert.Equal(300, profile.Dpi);
        Assert.Equal("Gray", profile.ColorMode);
        Assert.Equal("LZW", profile.CompressionType);
    }

    [Fact]
    public void OcrBinary300G4_DogruDegerlereDonmeli()
    {
        var profile = _factory.GetProfile(ConversionProfileKind.OcrBinary300G4);

        Assert.Equal(ConversionProfileKind.OcrBinary300G4, profile.Kind);
        Assert.Equal(300, profile.Dpi);
        Assert.Equal("Binary", profile.ColorMode);
        Assert.Equal("G4", profile.CompressionType);
    }

    [Fact]
    public void ArchiveColor300Lzw_DogruDegerlereDonmeli()
    {
        var profile = _factory.GetProfile(ConversionProfileKind.ArchiveColor300Lzw);

        Assert.Equal(ConversionProfileKind.ArchiveColor300Lzw, profile.Kind);
        Assert.Equal(300, profile.Dpi);
        Assert.Equal("Color", profile.ColorMode);
        Assert.Equal("LZW", profile.CompressionType);
    }

    [Fact]
    public void GecersizProfilKind_ArgumentOutOfRangeException_Firlatmali()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _factory.GetProfile((ConversionProfileKind)99));
    }
}