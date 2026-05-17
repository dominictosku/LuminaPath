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
