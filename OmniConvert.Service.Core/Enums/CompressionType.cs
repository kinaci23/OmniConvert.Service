namespace OmniConvert.Service.Core.Enums;

/// <summary>
/// Desteklenen TIFF sıkıştırma algoritmaları.
/// G4 yalnızca Binary renk moduyla kullanılabilir (ITU-T faks standardı).
/// </summary>
public enum CompressionType
{
    G4 = 0,
    LZW = 1
}