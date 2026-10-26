using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaPath.Tests.Services.Storage;

public sealed class StorageMigrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "storage-migration-" + Guid.NewGuid());
    private readonly StorageMigrationService _migration = new(NullLogger<StorageMigrationService>.Instance);

    [Fact]
    public async Task RoundTrip_PreservesExactNamesTypesEmptyFilesAndNonSeekableDownloads()
    {
        var source = new MemoryStorage();
        source.Files["extensionless"] = ([1, 2, 3], "image/png");
        source.Files["empty.bin"] = ([], "application/x-custom");
        var filesystem = new FileSystemStorage(_directory, NullLogger<FileSystemStorage>.Instance);
        var dryRun = await _migration.MigrateAsync(source, filesystem);
        Assert.Equal(new StorageMigrationResult(2, 0, true), dryRun);
        Assert.Empty(await filesystem.ListAsync());
        Assert.Equal(2, (await _migration.MigrateAsync(source, filesystem, false)).Copied);
        Assert.False(File.Exists(Path.Combine(_directory, "extensionless.png")));
        Assert.Equal(2, (await filesystem.ListAsync()).Count);
        // Read through a fresh adapter to check persisted metadata, rather than instance state.
        var reopened = new FileSystemStorage(_directory, NullLogger<FileSystemStorage>.Instance);
        var target = new MemoryStorage();
        Assert.Equal(2, (await _migration.MigrateAsync(reopened, target, false)).Copied);
        foreach (var (name, file) in source.Files)
        {
            Assert.Equal(file.Bytes, target.Files[name].Bytes);
            Assert.Equal(file.Type, target.Files[name].Type);
        }
        Assert.Equal(2, (await _migration.MigrateAsync(source, filesystem, false)).Skipped);
        Assert.Equal(0, source.Deletes);
    }

    [Fact]
    public async Task FailedRun_ResumesWithoutReuploadingVerifiedFiles()
    {
        var source = new MemoryStorage();
        source.Files["a.bin"] = ([1], "application/octet-stream");
        source.Files["b.bin"] = ([2], "application/octet-stream");
        var target = new MemoryStorage { FailName = "b.bin" };
        await Assert.ThrowsAsync<IOException>(() => _migration.MigrateAsync(source, target, false));
        Assert.Single(target.Files);
        target.FailName = null;
        Assert.Equal(new StorageMigrationResult(1, 1, false), await _migration.MigrateAsync(source, target, false));
        Assert.Equal(2, source.Files.Count);
        Assert.Equal(0, source.Deletes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistingConflict_StopsWithoutOverwriting(bool differentBytes)
    {
        var source = new MemoryStorage();
        source.Files["file"] = ([1], "image/png");
        var target = new MemoryStorage();
        target.Files["file"] = (differentBytes ? [2] : [1], differentBytes ? "image/png" : "text/plain");
        await Assert.ThrowsAsync<IOException>(() => _migration.MigrateAsync(source, target, false));
        Assert.Equal(0, target.Uploads);
        Assert.Equal(0, target.Deletes);
    }

    [Fact]
    public async Task CorruptUpload_IsDetectedAndSourceRetained()
    {
        var source = new MemoryStorage();
        source.Files["file"] = ([1], "image/png");
        var target = new MemoryStorage { Corrupt = true };
        await Assert.ThrowsAsync<IOException>(() => _migration.MigrateAsync(source, target, false));
        Assert.Equal(new byte[] { 1 }, source.Files["file"].Bytes);
        Assert.Equal(0, source.Deletes);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("nested/file")]
    public async Task AmbiguousOrNestedNames_AreRejectedBeforeWriting(string name)
    {
        var source = new MemoryStorage { DuplicateListing = name == "duplicate" };
        source.Files[name] = ([1], "image/png");
        var target = new MemoryStorage();
        await Assert.ThrowsAnyAsync<Exception>(() => _migration.MigrateAsync(source, target, false));
        Assert.Equal(0, target.Uploads);
    }

    [Fact]
    public async Task MissingSourceAndUnreadableDestination_DoNotBecomeSuccessfulCopies()
    {
        var source = new MemoryStorage { Missing = true };
        source.Files["file"] = ([1], "image/png");
        var target = new MemoryStorage();
        await Assert.ThrowsAsync<IOException>(() => _migration.MigrateAsync(source, target, false));
        source.Missing = false;
        target.Files["file"] = ([1], "image/png");
        target.Missing = true;
        await Assert.ThrowsAsync<IOException>(() => _migration.MigrateAsync(source, target, false));
        Assert.Equal(0, target.Uploads);
    }

    [Fact]
    public async Task SameProviderAndCancellation_AreRejected()
    {
        var source = new MemoryStorage();
        source.Files["file"] = ([1], "image/png");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _migration.MigrateAsync(source, source, false));
        var target = new MemoryStorage();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => _migration.MigrateAsync(source, target, false, cancellation.Token));
        Assert.Equal(0, target.Uploads);
    }

    [Fact]
    public async Task Registration_EnablesNewProviderWithoutChangingMigrationEngine()
    {
        var source = new MemoryStorage();
        source.Files["file"] = ([1], "image/png");
        var target = new MemoryStorage();
        var services = new ServiceCollection();
        services.AddStorageProvider("CustomSource", _ => source);
        services.AddStorageProvider("FutureProvider", _ => target);
        services.AddStorageProvider("Unused", _ => throw new InvalidOperationException("Must stay lazy"));
        services.AddSingleton<StorageProviderRegistry>();
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<StorageProviderRegistry>();
        Assert.Equal(1, (await _migration.MigrateAsync(registry.Get("customsource"), registry.Get("FutureProvider"), false)).Copied);
        Assert.Throws<InvalidOperationException>(() => registry.Get("unknown"));
        Assert.Throws<InvalidOperationException>(() => services.AddStorageProvider("CUSTOMSOURCE", _ => source));
    }

    [Fact]
    public async Task FileSystem_RejectsLinksAndNeverOverwritesExistingFiles()
    {
        if (OperatingSystem.IsWindows()) return;
        var source = new FileSystemStorage(_directory, NullLogger<FileSystemStorage>.Instance);
        await source.UploadExactAsync(new MemoryStream([1]), "original", "image/png");
        Assert.True((await source.UploadExactAsync(new MemoryStream([2]), "original", "image/png")).Error);
        File.CreateSymbolicLink(Path.Combine(_directory, "link"), Path.Combine(_directory, "original"));
        var target = new MemoryStorage();
        await Assert.ThrowsAsync<IOException>(() => _migration.MigrateAsync(source, target, false));
        Assert.Equal(0, target.Uploads);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private sealed class MemoryStorage : IStorageService
    {
        public Dictionary<string, (byte[] Bytes, string Type)> Files { get; } = new(StringComparer.Ordinal);
        public int Uploads { get; private set; }
        public int Deletes { get; private set; }
        public string? FailName { get; set; }
        public bool Corrupt { get; set; }
        public bool DuplicateListing { get; set; }
        public bool Missing { get; set; }
        public Task<List<BlobDto>> ListAsync()
        {
            var files = Files.Select(file => new BlobDto { Name = file.Key, ContentType = file.Value.Type }).ToList();
            if (DuplicateListing) files.Add(files[0]);
            return Task.FromResult(files);
        }
        public Task<BlobDto?> DownloadAsync(string name) => Task.FromResult<BlobDto?>(
            !Missing && Files.TryGetValue(name, out var file)
                ? new BlobDto { Name = name, ContentType = file.Type, Content = new NonSeekableStream(file.Bytes) }
                : null);
        public Task<BlobResponseDto> UploadAsync(Stream stream, string name, string? contentType = null) =>
            throw new InvalidOperationException("Migration must use exact-name uploads.");
        public async Task<BlobResponseDto> UploadExactAsync(Stream stream, string name, string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            Uploads++;
            if (Files.ContainsKey(name) || FailName == name) return new BlobResponseDto { Error = true };
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            Files.Add(name, (Corrupt ? [9] : buffer.ToArray(), contentType ?? "application/octet-stream"));
            return new BlobResponseDto { Blob = new BlobDto { Name = name } };
        }
        public Task<BlobResponseDto> DeleteAsync(string name) { Deletes++; throw new InvalidOperationException("Never delete."); }
        public Task<bool> RenameAsync(string oldName, string newName) => throw new InvalidOperationException("Never rename.");
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public override bool CanSeek => false;
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }
}
