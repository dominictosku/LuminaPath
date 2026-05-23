namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// Master switch for ALL automatic background jobs (DB backup, orphaned
    /// blob janitor, future scheduled jobs). When false, the scheduler still
    /// runs cleanup/maintenance work, but no new scheduled jobs are enqueued.
    /// Admins can always trigger ad-hoc runs via <see cref="BackgroundJobService"/>.
    /// </summary>
    public bool ScheduledJobsEnabled { get; set; }
    public int BackupIntervalHours { get; set; } = 24;
    public int BackupRetentionCount { get; set; } = 10;
    public int JobHistoryRetentionDays { get; set; } = 30;
    public int MaintenanceIntervalHours { get; set; } = 6;
    /// <summary>
    /// How often the orphaned-blob janitor runs when scheduled jobs are
    /// enabled. Default 24 h. Set to 0 to disable scheduled runs of THIS
    /// job specifically while leaving the master switch on.
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
