namespace OmniConvert.Service.Core.ValueObjects;

/// <summary>Bir iş için hazırlanan depolama yollarını gruplar.</summary>
public record StoragePathSet(
    string InputPath,
    string OutputPath,
    string WorkspacePath
);