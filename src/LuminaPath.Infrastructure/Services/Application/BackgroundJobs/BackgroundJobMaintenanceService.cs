namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobMaintenanceService
{
    private readonly BackgroundJobSettingsResolver _settingsResolver;
    private readonly BackgroundJobService _jobService;
    private readonly DatabaseBackupService _backupService;

    public BackgroundJobMaintenanceService(
        BackgroundJobSettingsResolver settingsResolver,
        BackgroundJobService jobService,
        DatabaseBackupService backupService)
    {
        _settingsResolver = settingsResolver;
        _jobService = jobService;
        _backupService = backupService;
    }

    public async Task<BackgroundMaintenanceResult> RunCleanupAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsResolver.GetAsync(cancellationToken);
        var deletedJobs = await _jobService.CleanupHistoryAsync(settings.JobHistoryRetentionDays, cancellationToken);
        var deletedBackups = await _backupService.DeleteOldBackupsAsync(settings.BackupRetentionCount, cancellationToken);

        return new BackgroundMaintenanceResult(deletedJobs, deletedBackups);
    }

    public async Task<BackgroundMaintenancePreviewResult> PreviewCleanupAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsResolver.GetAsync(cancellationToken);
        var oldJobs = await _jobService.CountHistoryCleanupCandidatesAsync(settings.JobHistoryRetentionDays, cancellationToken);
        var oldBackups = await _backupService.CountOldBackupsAsync(settings.BackupRetentionCount, cancellationToken);

        return new BackgroundMaintenancePreviewResult(oldJobs, oldBackups);
    }
}

public sealed record BackgroundMaintenanceResult(int DeletedJobs, int DeletedBackups);

public sealed record BackgroundMaintenancePreviewResult(int OldJobRecords, int OldBackupFiles);
