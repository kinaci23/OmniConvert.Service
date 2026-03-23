namespace OmniConvert.Service.Core.Enums;

public enum FailureCategory
{
    None = 0,
    Validation = 1,
    Timeout = 2,
    ExternalProcess = 3,
    UnsupportedFormat = 4,
    Conversion = 5,
    Storage = 6,
    Unknown = 7
}