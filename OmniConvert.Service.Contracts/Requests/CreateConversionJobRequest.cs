namespace OmniConvert.Service.Contracts.Requests;

/// <summary>
/// Yeni iş oluşturma isteği.
/// Sadece preset gönderilebilir, ya da preset + override kombinasyonu.
/// </summary>
/// <param name="FileName">Dönüştürülecek dosyanın adı (uzantı dahil).</param>
/// <param name="ProfileKind">Preset: OcrGray300Lzw | OcrBinary300G4 | ArchiveColor300Lzw</param>
/// <param name="Dpi">Opsiyonel DPI override. İzin verilenler: 150, 200, 300, 400, 600</param>
/// <param name="ColorMode">Opsiyonel renk modu override: Binary | Gray | Color</param>
/// <param name="Compression">Opsiyonel sıkıştırma override: G4 | LZW</param>
public record CreateConversionJobRequest(
    string FileName,
    string ProfileKind,
    int? Dpi = null,
    string? ColorMode = null,
    string? Compression = null
);