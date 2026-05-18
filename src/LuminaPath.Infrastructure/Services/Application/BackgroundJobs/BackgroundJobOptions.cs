namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool ScheduledBackupsEnabled { get; set; }
    public int BackupIntervalHours { get; set; } = 24;
    public int BackupRetentionCount { get; set; } = 10;
    public int JobHistoryRetentionDays { get; set; } = 30;
    public int MaintenanceIntervalHours { get; set; } = 6;

    internal static bool HasValidRanges(BackgroundJobOptions options)
    {
        return options.BackupIntervalHours is >= 1 and <= 720
            && options.BackupRetentionCount is >= 1 and <= 500
            && options.JobHistoryRetentionDays is >= 1 and <= 3650
            && options.MaintenanceIntervalHours is >= 1 and <= 720;
    }
}
