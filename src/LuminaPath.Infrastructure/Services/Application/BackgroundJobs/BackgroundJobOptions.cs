namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool ScheduledBackupsEnabled { get; set; }
    public int BackupIntervalHours { get; set; } = 24;
    public int BackupRetentionCount { get; set; } = 10;
    public int JobHistoryRetentionDays { get; set; } = 30;
    public int MaintenanceIntervalHours { get; set; } = 6;
}
