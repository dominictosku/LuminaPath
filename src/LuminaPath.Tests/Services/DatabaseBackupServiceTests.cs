using LuminaPath.Infrastructure.Services.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Test.Services;

public class DatabaseBackupServiceTests : IDisposable
{
    private readonly string _backupDirectory = Path.Combine(Path.GetTempPath(), $"luminapath-backups-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetBackupsAsync_ReturnsDumpFilesNewestFirst()
    {
        Directory.CreateDirectory(_backupDirectory);
        var oldBackup = Path.Combine(_backupDirectory, "old.dump");
        var newBackup = Path.Combine(_backupDirectory, "new.dump");
        var ignored = Path.Combine(_backupDirectory, "notes.txt");

        await File.WriteAllTextAsync(oldBackup, "old");
        await File.WriteAllTextAsync(newBackup, "new");
        await File.WriteAllTextAsync(ignored, "ignored");

        File.SetLastWriteTimeUtc(oldBackup, new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newBackup, new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc));

        var service = CreateService(CreateConfiguration("Host=localhost;Database=luminapath;Username=user;Password=password"));

        var backups = await service.GetBackupsAsync();

        Assert.Collection(
            backups,
            backup => Assert.Equal("new.dump", backup.FileName),
            backup => Assert.Equal("old.dump", backup.FileName));
    }

    [Fact]
    public async Task CreateBackupAsync_ReturnsFailure_WhenConnectionStringIsMissing()
    {
        var service = CreateService(CreateConfiguration(null));

        var result = await service.CreateBackupAsync();

        Assert.False(result.IsSuccess);
        var error = result.Match(_ => string.Empty, failure => string.Join("; ", failure.errorMessage));
        Assert.Equal("Missing database connection string.", error);
    }

    [Fact]
    public async Task CreateBackupAsync_ReturnsFailure_WhenPgDumpIsMissing()
    {
        var service = CreateService(
            CreateConfiguration("Host=localhost;Database=luminapath;Username=user;Password=password"),
            pgDumpPath: Path.Combine(_backupDirectory, "missing-pg-dump"));

        var result = await service.CreateBackupAsync();

        Assert.False(result.IsSuccess);
        var error = result.Match(_ => string.Empty, failure => string.Join("; ", failure.errorMessage));
        Assert.Contains("pg_dump was not found", error);
        Assert.Empty(Directory.Exists(_backupDirectory)
            ? Directory.EnumerateFiles(_backupDirectory, "*.dump")
            : Array.Empty<string>());
    }

    [Fact]
    public async Task GetBackupAsync_ReturnsBackup_WhenFileExistsInBackupDirectory()
    {
        Directory.CreateDirectory(_backupDirectory);
        var filePath = Path.Combine(_backupDirectory, "luminapath.dump");
        await File.WriteAllTextAsync(filePath, "backup");

        var service = CreateService(CreateConfiguration("Host=localhost;Database=luminapath;Username=user;Password=password"));

        var backup = await service.GetBackupAsync("luminapath.dump");

        Assert.NotNull(backup);
        Assert.Equal("luminapath.dump", backup.FileName);
        Assert.Equal(filePath, backup.FullPath);
        Assert.Equal(6, backup.SizeBytes);
    }

    [Theory]
    [InlineData("../outside.dump")]
    [InlineData("nested/outside.dump")]
    [InlineData("notes.txt")]
    [InlineData("")]
    public async Task GetBackupAsync_ReturnsNull_ForInvalidOrUnsafeFileName(string fileName)
    {
        Directory.CreateDirectory(_backupDirectory);
        await File.WriteAllTextAsync(Path.Combine(_backupDirectory, "notes.txt"), "not a backup");

        var service = CreateService(CreateConfiguration("Host=localhost;Database=luminapath;Username=user;Password=password"));

        var backup = await service.GetBackupAsync(fileName);

        Assert.Null(backup);
    }

    [Fact]
    public async Task DeleteOldBackupsAsync_KeepsNewestBackups()
    {
        Directory.CreateDirectory(_backupDirectory);
        var oldest = Path.Combine(_backupDirectory, "oldest.dump");
        var middle = Path.Combine(_backupDirectory, "middle.dump");
        var newest = Path.Combine(_backupDirectory, "newest.dump");

        await File.WriteAllTextAsync(oldest, "oldest");
        await File.WriteAllTextAsync(middle, "middle");
        await File.WriteAllTextAsync(newest, "newest");

        File.SetLastWriteTimeUtc(oldest, new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(middle, new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newest, new DateTime(2026, 1, 3, 8, 0, 0, DateTimeKind.Utc));

        var service = CreateService(CreateConfiguration("Host=localhost;Database=luminapath;Username=user;Password=password"));

        var deleted = await service.DeleteOldBackupsAsync(2);

        Assert.Equal(1, deleted);
        Assert.False(File.Exists(oldest));
        Assert.True(File.Exists(middle));
        Assert.True(File.Exists(newest));
    }

    public void Dispose()
    {
        if (Directory.Exists(_backupDirectory))
        {
            Directory.Delete(_backupDirectory, recursive: true);
        }
    }

    private DatabaseBackupService CreateService(IConfiguration configuration, string pgDumpPath = "pg_dump")
    {
        return new DatabaseBackupService(
            configuration,
            Options.Create(new DatabaseBackupOptions
            {
                Directory = _backupDirectory,
                PgDumpPath = pgDumpPath,
                MaxListedBackups = 20
            }),
            new Mock<ILogger<DatabaseBackupService>>().Object);
    }

    private static IConfiguration CreateConfiguration(string? connectionString)
    {
        var values = new Dictionary<string, string?>();
        if (connectionString is not null)
        {
            values["ConnectionStrings:Default"] = connectionString;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
