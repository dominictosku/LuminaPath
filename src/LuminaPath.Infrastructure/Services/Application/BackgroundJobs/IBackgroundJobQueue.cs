namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public interface IBackgroundJobQueue
{
    ValueTask QueueAsync(int jobId, CancellationToken cancellationToken = default);
    ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
}
