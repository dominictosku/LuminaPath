using LuminaPath.Core.Entities.Results;
using LuminaPath.Infrastructure.Services.Auditing;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class DatabaseBackupJobRunner : IBackgroundJobRunner
{
    private readonly DatabaseBackupService _backupService;
    private readonly AuditLogService? _auditLog;

    public DatabaseBackupJobRunner(DatabaseBackupService backupService)
        : this(backupService, null)
    {
    }

    public DatabaseBackupJobRunner(DatabaseBackupService backupService, AuditLogService? auditLog)
    {
        _backupService = backupService;
        _auditLog = auditLog;
    }

    public string JobType => BackgroundJobTypes.DatabaseBackup;

    public async Task<string?> RunAsync(string? payload, CancellationToken cancellationToken)
    {
        var result = await _backupService.CreateBackupAsync(cancellationToken);
        return await result.Match(
            backup => RecordBackupCreatedAsync(backup, cancellationToken),
            failure => RecordBackupFailedAsync(failure, cancellationToken));
    }

    private async Task<string?> RecordBackupCreatedAsync(DatabaseBackupInfo backup, CancellationToken cancellationToken)
    {
        if (_auditLog is not null)
        {
            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.DatabaseBackupCreated,
                Outcome = AuditOutcomes.Success,
                Actor = AuditActor.System,
                TargetType = "DatabaseBackup",
                TargetId = backup.FileName,
                TargetName = backup.FileName,
                Metadata = new
                {
                    sizeBytes = backup.SizeBytes,
                    createdAt = backup.CreatedAt
                }
            }, cancellationToken);
        }

        return $"Created backup {backup.FileName}";
    }

    private async Task<string?> RecordBackupFailedAsync(FailedResult failure, CancellationToken cancellationToken)
    {
        var message = string.Join("; ", failure.errorMessage);
        if (_auditLog is not null)
        {
            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.DatabaseBackupCreated,
                Outcome = AuditOutcomes.Failure,
                Actor = AuditActor.System,
                TargetType = "DatabaseBackup",
                ErrorMessage = message
            }, cancellationToken);
        }

        throw new InvalidOperationException(message);
    }
}
