using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;

namespace Test.Services;

public sealed class OrphanedBlobCleanupServiceTests : IDisposable
{
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"luminapath-cleanup-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunAsync_DeletesBlobsWithNoDocumentReference()
    {
        var storage = CreateStorage();
        await UploadBlob(storage, "referenced.png", "image/png");
        await UploadBlob(storage, "orphan-one.png", "image/png");
        await UploadBlob(storage, "orphan-two.jpg", "image/jpeg");

        var dbOptions = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(dbOptions))
        {
            context.MediaDocuments.Add(new MediaDocument
            {
                Name = "Cover",
                StorageName = "referenced.png",
                ContentType = "image/png",
                DocumentType = DocumentType.Image,
            });
            await context.SaveChangesAsync();
        }

        var service = CreateService(storage, dbOptions);

        var result = await service.RunAsync();

        Assert.Equal(3, result.Inspected);
        Assert.Equal(2, result.Deleted);
        Assert.Equal(0, result.Failed);
        Assert.True(File.Exists(Path.Combine(_storagePath, "referenced.png")));
        Assert.False(File.Exists(Path.Combine(_storagePath, "orphan-one.png")));
        Assert.False(File.Exists(Path.Combine(_storagePath, "orphan-two.jpg")));
    }

    [Fact]
    public async Task RunAsync_TreatsLegacyDocumentsThatStoreBlobNameOnlyInName_AsReferenced()
    {
        // Some historical Document rows have StorageName=null and the blob
        // name lives in Name. The janitor must respect that — otherwise it
        // would delete real cover art on the first run.
        var storage = CreateStorage();
        await UploadBlob(storage, "legacy.png", "image/png");

        var dbOptions = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(dbOptions))
        {
            context.MediaDocuments.Add(new MediaDocument
            {
                Name = "legacy.png",
                StorageName = null,
                ContentType = "image/png",
                DocumentType = DocumentType.Image,
            });
            await context.SaveChangesAsync();
        }

        var service = CreateService(storage, dbOptions);

        var result = await service.RunAsync();

        Assert.Equal(1, result.Inspected);
        Assert.Equal(0, result.Deleted);
        Assert.True(File.Exists(Path.Combine(_storagePath, "legacy.png")));
    }

    [Fact]
    public async Task RunAsync_WithNoBlobs_ReturnsEmptyResultWithoutTouchingTheDatabase()
    {
        var storage = CreateStorage();
        var dbOptions = Utilities.DbContext.TestDbContextOptions();

        var service = CreateService(storage, dbOptions);

        var result = await service.RunAsync();

        Assert.Equal(OrphanedBlobCleanupResult.Empty, result);
    }

    [Fact]
    public async Task OrphanedBlobCleanupJobRunner_ReturnsSummaryMessage()
    {
        var storage = CreateStorage();
        await UploadBlob(storage, "alone.png", "image/png");

        var dbOptions = Utilities.DbContext.TestDbContextOptions();
        var runner = new OrphanedBlobCleanupJobRunner(CreateService(storage, dbOptions));

        var message = await runner.RunAsync(null, CancellationToken.None);

        Assert.Equal("Scanned 1 blob(s), deleted 1 orphan(s), 0 failure(s).", message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    private OrphanedBlobCleanupService CreateService(
        FileSystemStorage storage,
        DbContextOptions<LuminaPathDbContext> dbOptions)
    {
        return new OrphanedBlobCleanupService(
            storage,
            new TestDbContextFactory(dbOptions),
            new Mock<ILogger<OrphanedBlobCleanupService>>().Object);
    }

    private FileSystemStorage CreateStorage()
    {
        return new FileSystemStorage(_storagePath, new Mock<ILogger<FileSystemStorage>>().Object);
    }

    private static async Task UploadBlob(FileSystemStorage storage, string name, string contentType)
    {
        await using var content = new MemoryStream([1, 2, 3]);
        var result = await storage.UploadAsync(content, name, contentType);
        Assert.False(result.Error);
    }
}
