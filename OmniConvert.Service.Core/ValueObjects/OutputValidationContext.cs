namespace OmniConvert.Service.Core.ValueObjects;

/// <summary>Çıktı doğrulaması için gereken bağlam bilgisi.</summary>
public record OutputValidationContext(
    string OutputFilePath,
    long? ExpectedMinFileSizeBytes = null
);