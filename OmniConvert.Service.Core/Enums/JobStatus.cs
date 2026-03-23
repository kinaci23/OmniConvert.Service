namespace OmniConvert.Service.Core.Enums;

public enum JobStatus
{
    Created = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    CompletedWithFallback = 6
}