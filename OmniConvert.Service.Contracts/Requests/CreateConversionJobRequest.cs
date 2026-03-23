namespace OmniConvert.Service.Contracts.Requests;

/// <param name="FileName">Dönüştürülecek dosyanın adı (uzantı dahil).</param>
/// <param name="ProfileKind">Profil adı: OcrGray300Lzw | OcrBinary300G4 | ArchiveColor300Lzw</param>
public record CreateConversionJobRequest(
    string FileName,
    string ProfileKind
);