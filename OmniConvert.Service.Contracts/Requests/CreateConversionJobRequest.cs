namespace OmniConvert.Service.Contracts.Requests;

using OmniConvert.Service.Core.Enums;

public record CreateConversionJobRequest(
    string FileName,
    ConversionProfileKind ProfileKind,
    int? Dpi = null,
    ColorMode? ColorMode = null,
    CompressionType? Compression = null,
    string? SourceFilePath = null
);