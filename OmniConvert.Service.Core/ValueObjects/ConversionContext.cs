namespace OmniConvert.Service.Core.ValueObjects;

using OmniConvert.Service.Core.Enums;

/// <summary>Pipeline'a geçirilen dönüşüm bağlamı; değişmez bir snapshot.</summary>
public record ConversionContext(
    Guid JobId,
    string InputFilePath,
    string OutputFilePath,
    string WorkspacePath,
    SourceFormat SourceFormat,
    ConversionProfile Profile
);