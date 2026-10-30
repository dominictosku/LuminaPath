using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Test.Utilities;

namespace LuminaPath.Tests.Services.Storage;

public sealed class StorageReconciliationServiceTests
{
    private readonly Mock<IStorageService> _storage = new();
    private readonly StorageOptions _options = new() { Provider = "FileSystem", FallbackProvider = "GoogleDrive" };

    [Fact]
    public async Task ReconcileAsync_ReturnsHealthy_WhenAllMatched()
    {
        var dbOptions = Test.Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(dbOptions))
        {
            context.MediaDocuments.Add(new MediaDocument
            {
                Name = "cover.png",
                StorageName = "uuid-cover.png",
                FileSizeBytes = 1024,
                DocumentType = DocumentType.Image
            });
            await context.SaveChangesAsync();
        }

        _storage.Setup(s => s.ListAsync()).ReturnsAsync(new List<BlobDto>
        {
            new() { Name = "uuid-cover.png", SizeBytes = 1024, LastModified = DateTimeOffset.UtcNow.AddDays(-2) }
        });

        var service = new StorageReconciliationService(_storage.Object, new TestDbContextFactory(dbOptions), Options.Create(_options));
        var report = await service.ReconcileAsync();

        Assert.True(report.IsHealthy);
        Assert.Equal(1, report.TotalDatabaseDocuments);
        Assert.Equal(1, report.TotalStorageFiles);
        Assert.Equal(1024, report.TotalStorageBytes);
        Assert.Empty(report.GhostDocuments);
        Assert.Empty(report.OrphanBlobs);
    }

    [Fact]
    public async Task ReconcileAsync_DetectsGhostsAndOrphansWithGracePeriod()
    {
        var dbOptions = Test.Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(dbOptions))
        {
            context.MediaDocuments.Add(new MediaDocument
            {
                Name = "missing.png",
                StorageName = "missing-uuid.png",
                FileSizeBytes = 500,
                DocumentType = DocumentType.Image
            });
            await context.SaveChangesAsync();
        }

        _storage.Setup(s => s.ListAsync()).ReturnsAsync(new List<BlobDto>
        {
            new() { Name = "old-orphan.png", SizeBytes = 200, LastModified = DateTimeOffset.UtcNow.AddHours(-30) },
            new() { Name = "fresh-orphan.png", SizeBytes = 300, LastModified = DateTimeOffset.UtcNow.AddHours(-2) }
        });

        var service = new StorageReconciliationService(_storage.Object, new TestDbContextFactory(dbOptions), Options.Create(_options));
        var report = await service.ReconcileAsync();

        Assert.False(report.IsHealthy);
        Assert.Single(report.GhostDocuments);
        Assert.Equal("missing-uuid.png", report.GhostDocuments[0].StorageName);

        Assert.Equal(2, report.OrphanBlobs.Count);
        Assert.Equal(1, report.EligibleOrphanCount);
        Assert.Equal(1, report.GracePeriodOrphanCount);
    }
}
