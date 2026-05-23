namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class OrphanedBlobCleanupJobRunner : IBackgroundJobRunner
{
    private readonly OrphanedBlobCleanupService _cleanupService;

    public OrphanedBlobCleanupJobRunner(OrphanedBlobCleanupService cleanupService)
    {
        _cleanupService = cleanupService;
    }

    public string JobType => BackgroundJobTypes.OrphanedBlobCleanup;

    public async Task<string?> RunAsync(string? payload, CancellationToken cancellationToken)
    {
        var result = await _cleanupService.RunAsync(cancellationToken);
        return $"Scanned {result.Inspected} blob(s), deleted {result.Deleted} orphan(s), {result.Failed} failure(s).";
    }
}
