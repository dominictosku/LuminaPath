using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class StorageReconciliationService
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);
    private readonly IStorageService _storage;
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IOptions<StorageOptions> _options;

    public StorageReconciliationService(
        IStorageService storage,
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IOptions<StorageOptions> options)
    {
        _storage = storage;
        _dbContextFactory = dbContextFactory;
        _options = options;
    }

    public async Task<StorageReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var documents = await context.Documents.AsNoTracking().ToListAsync(cancellationToken);
        var blobs = await _storage.ListAsync();

        var blobMap = new Dictionary<string, BlobDto>(StringComparer.Ordinal);
        long totalStorageBytes = 0;
        foreach (var blob in blobs)
        {
            if (!string.IsNullOrWhiteSpace(blob.Name))
            {
                blobMap[blob.Name] = blob;
                totalStorageBytes += blob.SizeBytes ?? 0;
            }
        }

        var referencedNames = new HashSet<string>(StringComparer.Ordinal);
        var ghosts = new List<GhostDocumentInfo>();
        long totalDatabaseBytes = 0;

        foreach (var doc in documents)
        {
            totalDatabaseBytes += doc.FileSizeBytes;
            var targetName = doc.StorageName ?? doc.Name;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                continue;
            }

            referencedNames.Add(targetName);
            if (!blobMap.ContainsKey(targetName))
            {
                ghosts.Add(new GhostDocumentInfo(doc.Id, doc.Name ?? "Untitled", targetName));
            }
        }

        var orphans = new List<OrphanBlobInfo>();
        var now = DateTimeOffset.UtcNow;
        foreach (var blob in blobs)
        {
            if (string.IsNullOrWhiteSpace(blob.Name) || referencedNames.Contains(blob.Name))
            {
                continue;
            }

            var inGrace = blob.LastModified.HasValue && blob.LastModified.Value > now - GracePeriod;
            orphans.Add(new OrphanBlobInfo(blob.Name, blob.SizeBytes, blob.LastModified, inGrace));
        }

        return new StorageReconciliationReport(
            _options.Value.Provider,
            _options.Value.FallbackProvider,
            documents.Count,
            totalDatabaseBytes,
            blobs.Count,
            totalStorageBytes,
            ghosts,
            orphans,
            now);
    }
}

public sealed record StorageReconciliationReport(
    string ActiveProvider,
    string? FallbackProvider,
    int TotalDatabaseDocuments,
    long TotalDatabaseBytes,
    int TotalStorageFiles,
    long TotalStorageBytes,
    IReadOnlyList<GhostDocumentInfo> GhostDocuments,
    IReadOnlyList<OrphanBlobInfo> OrphanBlobs,
    DateTimeOffset ScannedAt)
{
    public bool IsHealthy => GhostDocuments.Count == 0 && OrphanBlobs.Count == 0;
    public int EligibleOrphanCount => OrphanBlobs.Count(o => !o.InGracePeriod);
    public int GracePeriodOrphanCount => OrphanBlobs.Count(o => o.InGracePeriod);
}

public sealed record GhostDocumentInfo(int DocumentId, string Name, string StorageName);
public sealed record OrphanBlobInfo(string Name, long? SizeBytes, DateTimeOffset? LastModified, bool InGracePeriod);
