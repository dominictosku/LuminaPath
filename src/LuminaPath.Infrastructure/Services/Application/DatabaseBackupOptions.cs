namespace LuminaPath.Infrastructure.Services.Application;

public sealed class DatabaseBackupOptions
{
    public const string SectionName = "DatabaseBackup";

    public string Directory { get; set; } = "App_Data/storage/backups";
    public string PgDumpPath { get; set; } = "pg_dump";
    public string FilePrefix { get; set; } = "luminapath";
    public int MaxListedBackups { get; set; } = 20;
}
