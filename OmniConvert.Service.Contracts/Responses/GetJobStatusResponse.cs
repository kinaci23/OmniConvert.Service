namespace OmniConvert.Service.Contracts.Responses;

public record GetJobStatusResponse(
    Guid JobId,
    string Status,
    string FileName,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage
);