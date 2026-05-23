using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundJobSchedulerService> _logger;

    public BackgroundJobSchedulerService(IServiceScopeFactory scopeFactory, ILogger<BackgroundJobSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromHours(6);

            try
            {
                delay = await RunMaintenanceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background job scheduler failed.");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<TimeSpan> RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsResolver = scope.ServiceProvider.GetRequiredService<BackgroundJobSettingsResolver>();
        var jobService = scope.ServiceProvider.GetRequiredService<BackgroundJobService>();
        var maintenanceService = scope.ServiceProvider.GetRequiredService<BackgroundJobMaintenanceService>();
        var settings = await settingsResolver.GetAsync(cancellationToken);

        if (settings.ScheduledBackupsEnabled && await ShouldRunBackupAsync(jobService, settings, cancellationToken))
        {
            var job = await jobService.EnqueueDatabaseBackupAsync(cancellationToken);
            _logger.LogInformation("Scheduled database backup job {JobId} is {Status}.", job.Id, job.Status);
        }

        if (settings.OrphanedBlobCleanupIntervalHours > 0
            && await ShouldRunOrphanedBlobCleanupAsync(jobService, settings, cancellationToken))
        {
            var job = await jobService.EnqueueOrphanedBlobCleanupAsync(cancellationToken);
            _logger.LogInformation("Scheduled orphaned blob cleanup job {JobId} is {Status}.", job.Id, job.Status);
        }

        var cleanup = await maintenanceService.RunCleanupAsync(cancellationToken);

        if (cleanup.DeletedJobs > 0 || cleanup.DeletedBackups > 0)
        {
            _logger.LogInformation("Background maintenance removed {JobCount} old jobs and {BackupCount} old backups.", cleanup.DeletedJobs, cleanup.DeletedBackups);
        }

        return TimeSpan.FromHours(settings.MaintenanceIntervalHours);
    }

    private static async Task<bool> ShouldRunBackupAsync(
        BackgroundJobService jobService,
        BackgroundJobRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        var lastSuccess = await jobService.GetLastSuccessfulJobAsync(BackgroundJobTypes.DatabaseBackup, cancellationToken);
        if (lastSuccess?.CompletedAt is null)
        {
            return true;
        }

        return lastSuccess.CompletedAt.Value <= DateTime.UtcNow.AddHours(-settings.BackupIntervalHours);
    }

    private static async Task<bool> ShouldRunOrphanedBlobCleanupAsync(
        BackgroundJobService jobService,
        BackgroundJobRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        var lastSuccess = await jobService.GetLastSuccessfulJobAsync(
            BackgroundJobTypes.OrphanedBlobCleanup,
            cancellationToken);
        if (lastSuccess?.CompletedAt is null)
        {
            return true;
        }

        return lastSuccess.CompletedAt.Value
            <= DateTime.UtcNow.AddHours(-settings.OrphanedBlobCleanupIntervalHours);
    }
}
