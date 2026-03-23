namespace OmniConvert.Service.Contracts.Responses;

public record CreateConversionJobResponse(
    Guid JobId,
    string Status,
    string FileName
);