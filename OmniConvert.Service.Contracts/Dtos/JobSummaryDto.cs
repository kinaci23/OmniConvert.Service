namespace OmniConvert.Service.Contracts.Dtos;

public record JobSummaryDto(
    Guid JobId,
    string FileName,
    string Status,
    string SourceFormat,
    string ProfileKind,
    DateTime CreatedAtUtc
);