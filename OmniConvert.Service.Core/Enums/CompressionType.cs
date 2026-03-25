namespace OmniConvert.Service.Core.Enums;

/// <summary>
/// Desteklenen TIFF sıkıştırma algoritmaları.
/// G4 yalnızca Binary renk moduyla kullanılabilir (ITU-T T.6 faks standardı).
/// Jpeg yalnızca Color renk moduyla kullanılabilir.
/// </summary>
public enum CompressionType
{
    None = 0,
    G4 = 1,
    LZW = 2,
    Jpeg = 3
}