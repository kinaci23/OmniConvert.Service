namespace OmniConvert.Service.Application.Validation;

using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

/// <summary>
/// Minimum TIFF çıktı doğrulaması:
/// 1. Path boş değil
/// 2. Uzantı .tif veya .tiff
/// 3. Dosya var
/// 4. Dosya geçerli TIFF magic bytes içeriyor
/// 5. En az 1 IFD (frame) var
///
/// DPI / compression / colormode kalite denetimi sonraki iterasyonda eklenecek.
/// </summary>
public class DefaultOutputValidator : IOutputValidator
{
    public Task<ValidationResult> ValidateAsync(
        OutputValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.OutputFilePath))
            return Task.FromResult(ValidationResult.Fail("Output path boş."));

        var ext = Path.GetExtension(context.OutputFilePath)
                      ?.TrimStart('.')
                      .ToLowerInvariant();

        if (ext is not ("tif" or "tiff"))
            return Task.FromResult(ValidationResult.Fail(
                $"Geçersiz uzantı: .{ext}. Beklenen: .tif veya .tiff"));

        if (!File.Exists(context.OutputFilePath))
            return Task.FromResult(ValidationResult.Fail(
                $"Dosya bulunamadı: {context.OutputFilePath}"));

        return Task.FromResult(ValidateTiffBytes(context.OutputFilePath));
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// TIFF magic bytes + IFD0 offset kontrolü.
    /// Herhangi bir bağımlılık olmadan raw byte okuma ile yapılır.
    ///
    /// TIFF yapısı:
    ///   Byte 0-1 : byte order  (II = little-endian, MM = big-endian)
    ///   Byte 2-3 : magic       (42 = 0x002A little / 0x2A00 big)
    ///   Byte 4-7 : IFD0 offset (0'dan büyük olmalı)
    /// </summary>
    private static ValidationResult ValidateTiffBytes(string filePath)
    {
        try
        {
            using var fs = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (fs.Length < 8)
                return ValidationResult.Fail("Dosya çok küçük, geçerli TIFF olamaz.");

            Span<byte> header = stackalloc byte[8];
            var read = fs.Read(header);

            if (read < 8)
                return ValidationResult.Fail("TIFF header okunamadı.");

            // Byte order tespiti
            bool isLittleEndian;
            if (header[0] == 0x49 && header[1] == 0x49) isLittleEndian = true;
            else if (header[0] == 0x4D && header[1] == 0x4D) isLittleEndian = false;
            else return ValidationResult.Fail("Geçersiz TIFF byte order marker.");

            // Magic number kontrolü (42)
            ushort magic = isLittleEndian
                ? (ushort)(header[2] | (header[3] << 8))
                : (ushort)((header[2] << 8) | header[3]);

            if (magic != 42)
                return ValidationResult.Fail($"Geçersiz TIFF magic number: {magic}. Beklenen: 42");

            // IFD0 offset kontrolü — en az 1 frame var mı?
            uint ifd0Offset = isLittleEndian
                ? (uint)(header[4] | (header[5] << 8) | (header[6] << 16) | (header[7] << 24))
                : (uint)((header[4] << 24) | (header[5] << 16) | (header[6] << 8) | header[7]);

            if (ifd0Offset == 0)
                return ValidationResult.Fail("TIFF IFD0 offset sıfır — frame içermiyor.");

            if (ifd0Offset >= (uint)fs.Length)
                return ValidationResult.Fail("TIFF IFD0 offset dosya boyutunu aşıyor — bozuk dosya.");

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"TIFF doğrulama hatası: {ex.Message}");
        }
    }
}