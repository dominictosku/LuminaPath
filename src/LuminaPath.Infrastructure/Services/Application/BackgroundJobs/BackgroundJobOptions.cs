namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool ScheduledBackupsEnabled { get; set; }
    public int BackupIntervalHours { get; set; } = 24;
    public int BackupRetentionCount { get; set; } = 10;
    public int JobHistoryRetentionDays { get; set; } = 30;
    public int MaintenanceIntervalHours { get; set; } = 6;
    /// <summary>
    /// How often the orphaned-blob janitor runs. Default 24 h. Set to 0
    /// to disable scheduled runs (admins can still trigger ad-hoc runs
    /// via the BackgroundJobService API).
    /// </summary>
    public int OrphanedBlobCleanupIntervalHours { get; set; } = 24;

    internal static bool HasValidRanges(BackgroundJobOptions options)
    {
        return options.BackupIntervalHours is >= 1 and <= 720
            && options.BackupRetentionCount is >= 1 and <= 500
            && options.JobHistoryRetentionDays is >= 1 and <= 3650
            && options.MaintenanceIntervalHours is >= 1 and <= 720
            && options.OrphanedBlobCleanupIntervalHours is >= 0 and <= 720;
    }
}
