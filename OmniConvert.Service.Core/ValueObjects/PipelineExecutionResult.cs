namespace OmniConvert.Service.Core.ValueObjects;

using OmniConvert.Service.Core.Enums;

/// <summary>Bir pipeline çalışmasının sonucunu temsil eder.</summary>
public record PipelineExecutionResult(
    bool Success,
    string? OutputPath,
    string? ErrorMessage = null,
    FailureCategory FailureCategory = FailureCategory.None
);