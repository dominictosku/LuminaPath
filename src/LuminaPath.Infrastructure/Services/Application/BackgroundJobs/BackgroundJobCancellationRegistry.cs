using System.Collections.Concurrent;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobCancellationRegistry : IBackgroundJobCancellationRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _runningJobs = new();

    public IDisposable Register(int jobId, CancellationTokenSource cancellationTokenSource)
    {
        _runningJobs[jobId] = cancellationTokenSource;
        return new Registration(_runningJobs, jobId);
    }

    public bool RequestCancellation(int jobId)
    {
        if (!_runningJobs.TryGetValue(jobId, out var cancellationTokenSource))
        {
            return false;
        }

        cancellationTokenSource.Cancel();
        return true;
    }

    private sealed class Registration : IDisposable
    {
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _runningJobs;
        private readonly int _jobId;

        public Registration(ConcurrentDictionary<int, CancellationTokenSource> runningJobs, int jobId)
        {
            _runningJobs = runningJobs;
            _jobId = jobId;
        }

        public void Dispose()
        {
            _runningJobs.TryRemove(_jobId, out _);
        }
    }
}
