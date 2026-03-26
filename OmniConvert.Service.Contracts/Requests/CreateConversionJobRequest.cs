namespace OmniConvert.Service.Contracts.Requests;

using OmniConvert.Service.Core.Enums;

/// <summary>
/// Yeni iş oluşturma isteği.
/// Dosya multipart/form-data olarak gönderilir.
/// Enum değerleri JSON'da string olarak yazılır.
/// </summary>
public record CreateConversionJobRequest(
    ConversionProfileKind ProfileKind,
    int? Dpi = null,
    ColorMode? ColorMode = null,
    CompressionType? Compression = null
);