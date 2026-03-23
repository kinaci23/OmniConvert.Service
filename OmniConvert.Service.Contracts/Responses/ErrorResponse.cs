namespace OmniConvert.Service.Contracts.Responses;

public record ErrorResponse(
    string Error,
    string? Detail = null
);