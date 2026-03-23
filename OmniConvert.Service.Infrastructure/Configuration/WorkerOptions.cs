namespace OmniConvert.Service.Infrastructure.Configuration;

public class WorkerOptions
{
    public const string SectionName = "Worker";

    public int ConcurrencyLevel { get; set; } = 2;
    public int PollingIntervalMs { get; set; } = 500;
}