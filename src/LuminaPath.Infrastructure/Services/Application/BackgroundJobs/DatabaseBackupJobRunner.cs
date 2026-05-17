namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class DatabaseBackupJobRunner : IBackgroundJobRunner
{
    private readonly DatabaseBackupService _backupService;

    public DatabaseBackupJobRunner(DatabaseBackupService backupService)
    {
        _backupService = backupService;
    }

    public string JobType => BackgroundJobTypes.DatabaseBackup;

    public async Task<string?> RunAsync(string? payload, CancellationToken cancellationToken)
    {
        var result = await _backupService.CreateBackupAsync(cancellationToken);
        return result.Match(
            backup => $"Created backup {backup.FileName}",
            failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));
    }
}
