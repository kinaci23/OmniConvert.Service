namespace OmniConvert.Service.Tests.Unit.Validation;

using ImageMagick;
using OmniConvert.Service.Application.Validation;
using OmniConvert.Service.Core.ValueObjects;
using Xunit;

public class OutputValidatorTests : IDisposable
{
    private readonly DefaultOutputValidator _validator = new();
    private readonly string _testDir;

    public OutputValidatorTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(), "OmniConvertValidatorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task GecerliTiff_ValidationBasarili_Olmali()
    {
        var tiffPath = CreateValidTiff("valid.tif");
        var result = await _validator.ValidateAsync(new OutputValidationContext(tiffPath));

        Assert.True(result.IsValid);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task GecerliTiffExtension_OlunarakGecerliTiff_ValidationBasarili()
    {
        var tiffPath = CreateValidTiff("valid.tiff");
        var result = await _validator.ValidateAsync(new OutputValidationContext(tiffPath));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task OlmayanDosya_ValidationBasarisiz_Olmali()
    {
        var path = Path.Combine(_testDir, "yok.tif");
        var result = await _validator.ValidateAsync(new OutputValidationContext(path));

        Assert.False(result.IsValid);
        Assert.Contains("bulunamadı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task YanlisUzanti_ValidationBasarisiz_Olmali()
    {
        var path = Path.Combine(_testDir, "output.pdf");
        await File.WriteAllTextAsync(path, "dummy");

        var result = await _validator.ValidateAsync(new OutputValidationContext(path));

        Assert.False(result.IsValid);
        Assert.Contains("uzantı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BosPpath_ValidationBasarisiz_Olmali()
    {
        var result = await _validator.ValidateAsync(new OutputValidationContext(""));

        Assert.False(result.IsValid);
        Assert.Contains("boş", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BozukDosya_TiffAcilamiyor_ValidationBasarisiz_Olmali()
    {
        var path = Path.Combine(_testDir, "bozuk.tif");
        // Geçersiz içerik — TIFF magic byte'ları yok
        await File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);

        var result = await _validator.ValidateAsync(new OutputValidationContext(path));

        Assert.False(result.IsValid);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task CokKucukDosya_ValidationBasarisiz_Olmali()
    {
        var path = Path.Combine(_testDir, "kucuk.tif");
        await File.WriteAllBytesAsync(path, [0x49, 0x49]); // sadece 2 byte

        var result = await _validator.ValidateAsync(new OutputValidationContext(path));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task LittleEndianTiff_ValidationBasarili_Olmali()
    {
        // Gerçek little-endian TIFF header — Magick.NET ile üretiliyor
        var tiffPath = CreateValidTiff("le.tif");
        var bytes = await File.ReadAllBytesAsync(tiffPath);

        // Byte order doğrula
        Assert.Equal(0x49, bytes[0]); // I
        Assert.Equal(0x49, bytes[1]); // I

        var result = await _validator.ValidateAsync(new OutputValidationContext(tiffPath));
        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------

    /// <summary>Magick.NET ile minimal geçerli TIFF oluşturur.</summary>
    private string CreateValidTiff(string fileName)
    {
        var path = Path.Combine(_testDir, fileName);
        using var image = new MagickImage(MagickColors.White, 16, 16);
        image.Format = MagickFormat.Tiff;
        image.Settings.Compression = CompressionMethod.LZW;
        image.Write(path);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}