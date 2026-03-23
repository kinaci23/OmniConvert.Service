namespace OmniConvert.Service.Contracts.Responses;

public record GetJobResultResponse(
    Guid JobId,
    string Status,
    string? OutputPath,
    bool UsedFallback,
    string? ErrorMessage
);