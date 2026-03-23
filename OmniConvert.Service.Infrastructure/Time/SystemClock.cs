namespace OmniConvert.Service.Infrastructure.Time;

using OmniConvert.Service.Core.Interfaces;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}