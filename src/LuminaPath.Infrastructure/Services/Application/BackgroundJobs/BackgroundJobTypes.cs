namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public static class BackgroundJobTypes
{
    public const string DatabaseBackup = "DatabaseBackup";
    public const string MaintenanceCleanup = "MaintenanceCleanup";
    public const string OrphanedBlobCleanup = "OrphanedBlobCleanup";
}
