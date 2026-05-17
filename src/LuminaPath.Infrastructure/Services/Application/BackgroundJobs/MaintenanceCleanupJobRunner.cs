namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class MaintenanceCleanupJobRunner : IBackgroundJobRunner
{
    private readonly BackgroundJobMaintenanceService _maintenanceService;

    public MaintenanceCleanupJobRunner(BackgroundJobMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    public string JobType => BackgroundJobTypes.MaintenanceCleanup;

    public async Task<string?> RunAsync(string? payload, CancellationToken cancellationToken)
    {
        var result = await _maintenanceService.RunCleanupAsync(cancellationToken);
        return $"Removed {result.DeletedJobs} old job records and {result.DeletedBackups} old backup files.";
    }
}
