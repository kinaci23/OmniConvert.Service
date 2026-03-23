namespace OmniConvert.Service.Infrastructure.Queue;

using System.Threading.Channels;
using OmniConvert.Service.Core.Interfaces;

public class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)
        => await _channel.Writer.WriteAsync(jobId, cancellationToken);

    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken = default)
        => await _channel.Reader.ReadAsync(cancellationToken);
}