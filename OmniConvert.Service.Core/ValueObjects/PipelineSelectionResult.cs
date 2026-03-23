namespace OmniConvert.Service.Core.ValueObjects;

using OmniConvert.Service.Core.Enums;

/// <summary>Seçilen birincil ve varsa yedek pipeline bilgisini taşır.</summary>
public record PipelineSelectionResult(
    PipelineKind Primary,
    PipelineKind? Fallback
);