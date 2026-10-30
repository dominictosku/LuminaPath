using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

/// <summary>
/// Periodic janitor that removes blobs from the configured storage backend
/// that are not referenced by any <see cref="LuminaPath.Core.Models.Base.Document"/>
/// row. Per-entity deletes already prune their own cover via
/// <see cref="ModelServices.DocumentService.DeleteMediaDocument"/> — this
/// service exists to clean up historical orphans (files left behind by
/// past bugs, aborted uploads, etc.) and to backstop any future leaks.
/// </summary>
public sealed class OrphanedBlobCleanupService
{
    public TimeSpan GracePeriod { get; set; }

    private readonly IStorageService _storage;
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly ILogger<OrphanedBlobCleanupService> _logger;
    private readonly AuditLogService? _auditLog;

    public OrphanedBlobCleanupService(
        IStorageService storage,
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        ILogger<OrphanedBlobCleanupService> logger,
        AuditLogService? auditLog = null,
        TimeSpan? gracePeriod = null)
    {
        _storage = storage;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _auditLog = auditLog;
        GracePeriod = gracePeriod ?? TimeSpan.FromHours(24);
    }

    public async Task<OrphanedBlobCleanupResult> RunAsync(CancellationToken cancellationToken = default)
    {
        return await ScanAsync(deleteOrphans: true, cancellationToken);
    }

    public async Task<OrphanedBlobCleanupPreviewResult> PreviewAsync(CancellationToken cancellationToken = default)
    {
        var result = await ScanAsync(deleteOrphans: false, cancellationToken);
        return new OrphanedBlobCleanupPreviewResult(result.Inspected, result.Deleted, result.OrphanedBlobNames);
    }

    private async Task<OrphanedBlobCleanupResult> ScanAsync(
        bool deleteOrphans,
        CancellationToken cancellationToken)
    {
        var blobs = await _storage.ListAsync();
        if (blobs.Count == 0)
        {
            return OrphanedBlobCleanupResult.Empty;
        }

        // Pull the full referenced-name set once so storage scans can use
        // O(1) lookups while deciding which blobs are safe to remove.
        var referencedNames = await GetReferencedStorageNamesAsync(cancellationToken);

        var inspected = 0;
        var deleted = 0;
        var failed = 0;
        var orphanedNames = new List<string>();

        foreach (var blob in blobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = blob.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            inspected++;

            if (referencedNames.Contains(name))
            {
                continue;
            }

            // Protect newly uploaded files that may be part of an in-flight form submission.
            if (blob.LastModified.HasValue && blob.LastModified.Value > DateTimeOffset.UtcNow - GracePeriod)
            {
                _logger.LogDebug(
                    "Blob {Blob} is unreferenced but was modified recently ({LastModified}). Preserving within grace period.",
                    name,
                    blob.LastModified.Value);
                continue;
            }

            orphanedNames.Add(name);
            if (!deleteOrphans)
            {
                deleted++;
                continue;
            }

            var result = await _storage.DeleteAsync(name);
            if (result.Error)
            {
                failed++;
                _logger.LogWarning(
                    "Orphaned blob {Blob} could not be deleted: {Status}",
                    name,
                    result.Status);
                await AuditOrphanAsync(name, AuditOutcomes.Failure, errorMessage: result.Status);
                continue;
            }

            deleted++;
            await AuditOrphanAsync(name, AuditOutcomes.Success);
        }

        if (deleted > 0 || failed > 0)
        {
            _logger.LogInformation(
                "Orphaned blob cleanup deleted {Deleted} blob(s) ({Failed} failed) out of {Inspected} scanned.",
                deleted,
                failed,
                inspected);
        }

        return new OrphanedBlobCleanupResult(inspected, deleted, failed, orphanedNames);
    }

    private async Task<HashSet<string>> GetReferencedStorageNamesAsync(CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var documents = await context.Documents
            .AsNoTracking()
            .Select(doc => new { doc.StorageName, doc.Name })
            .ToListAsync(cancellationToken);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var doc in documents)
        {
            if (!string.IsNullOrWhiteSpace(doc.StorageName)) names.Add(doc.StorageName);
            if (!string.IsNullOrWhiteSpace(doc.Name)) names.Add(doc.Name);
        }

        return names;
    }

    private Task AuditOrphanAsync(string blobName, string outcome, string? errorMessage = null)
    {
        if (_auditLog is null)
        {
            return Task.CompletedTask;
        }

        return _auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Document,
            Action = AuditActions.DocumentDeleted,
            Outcome = outcome,
            TargetType = "OrphanedBlob",
            TargetId = blobName,
            TargetName = blobName,
            Metadata = new
            {
                source = "OrphanedBlobCleanupService",
            },
            ErrorMessage = errorMessage,
        });
    }
}

public sealed record OrphanedBlobCleanupResult(
    int Inspected,
    int Deleted,
    int Failed,
    IReadOnlyList<string> OrphanedBlobNames)
{
    public static readonly OrphanedBlobCleanupResult Empty = new(0, 0, 0, Array.Empty<string>());
}

public sealed record OrphanedBlobCleanupPreviewResult(
    int Inspected,
    int WouldDelete,
    IReadOnlyList<string> OrphanedBlobNames);
