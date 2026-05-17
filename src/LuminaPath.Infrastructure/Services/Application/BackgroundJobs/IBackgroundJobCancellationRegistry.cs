namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public interface IBackgroundJobCancellationRegistry
{
    IDisposable Register(int jobId, CancellationTokenSource cancellationTokenSource);
    bool RequestCancellation(int jobId);
}
