namespace OmniConvert.Service.Contracts.Dtos;

public record JobResultDto(
    Guid JobId,
    string Status,
    string? OutputPath,
    bool UsedFallback,
    string? PipelineUsed,
    DateTime? CompletedAtUtc
);