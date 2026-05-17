using System.Threading.Channels;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<int> _queue = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask QueueAsync(int jobId, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(jobId, cancellationToken);
    }

    public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
