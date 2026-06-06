using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Test.Utilities;

namespace Test.Services;

public class BackgroundJobMaintenanceServiceTests : IDisposable
{
    private readonly string _backupDirectory = Path.Combine(Path.GetTempPath(), $"luminapath-maintenance-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunCleanupAsync_RemovesExpiredJobsAndOldBackups()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsBackupRetentionCount, Value = "1" },
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsJobHistoryRetentionDays, Value = "7" });
            context.BackgroundJobs.AddRange(
                new BackgroundJobRecord
                {
                    JobType = "Old",
                    DisplayName = "Old completed",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 5, 1, 10, 1, 0, DateTimeKind.Utc)
                },
                new BackgroundJobRecord
                {
                    JobType = "Recent",
                    DisplayName = "Recent completed",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 5, 16, 10, 1, 0, DateTimeKind.Utc)
                });
            await context.SaveChangesAsync();
        }

        Directory.CreateDirectory(_backupDirectory);
        var oldBackup = Path.Combine(_backupDirectory, "old.dump");
        var newBackup = Path.Combine(_backupDirectory, "new.dump");
        await File.WriteAllTextAsync(oldBackup, "old");
        await File.WriteAllTextAsync(newBackup, "new");
        File.SetLastWriteTimeUtc(oldBackup, new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newBackup, new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));

        var service = CreateMaintenanceService(options);

        var result = await service.RunCleanupAsync();

        Assert.Equal(1, result.DeletedJobs);
        Assert.Equal(1, result.DeletedBackups);
        Assert.False(File.Exists(oldBackup));
        Assert.True(File.Exists(newBackup));

        await using var assertContext = new LuminaPathDbContext(options);
        var remainingJob = await assertContext.BackgroundJobs.SingleAsync();
        Assert.Equal("Recent", remainingJob.JobType);
    }

    [Fact]
    public async Task PreviewCleanupAsync_CountsExpiredJobsAndOldBackupsWithoutDeleting()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsBackupRetentionCount, Value = "1" },
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsJobHistoryRetentionDays, Value = "7" });
            context.BackgroundJobs.AddRange(
                new BackgroundJobRecord
                {
                    JobType = "Old",
                    DisplayName = "Old completed",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 5, 1, 10, 1, 0, DateTimeKind.Utc)
                },
                new BackgroundJobRecord
                {
                    JobType = "Recent",
                    DisplayName = "Recent completed",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 5, 16, 10, 1, 0, DateTimeKind.Utc)
                });
            await context.SaveChangesAsync();
        }

        Directory.CreateDirectory(_backupDirectory);
        var oldBackup = Path.Combine(_backupDirectory, "old.dump");
        var newBackup = Path.Combine(_backupDirectory, "new.dump");
        await File.WriteAllTextAsync(oldBackup, "old");
        await File.WriteAllTextAsync(newBackup, "new");
        File.SetLastWriteTimeUtc(oldBackup, new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newBackup, new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));

        var service = CreateMaintenanceService(options);

        var preview = await service.PreviewCleanupAsync();

        Assert.Equal(1, preview.OldJobRecords);
        Assert.Equal(1, preview.OldBackupFiles);
        Assert.True(File.Exists(oldBackup));
        Assert.True(File.Exists(newBackup));

        await using var assertContext = new LuminaPathDbContext(options);
        Assert.Equal(2, await assertContext.BackgroundJobs.CountAsync());
    }

    [Fact]
    public async Task MaintenanceCleanupJobRunner_ReturnsSummaryMessage()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsBackupRetentionCount, Value = "1" },
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsJobHistoryRetentionDays, Value = "7" });
            await context.SaveChangesAsync();
        }

        Directory.CreateDirectory(_backupDirectory);
        await File.WriteAllTextAsync(Path.Combine(_backupDirectory, "only.dump"), "backup");

        var runner = new MaintenanceCleanupJobRunner(CreateMaintenanceService(options));

        var message = await runner.RunAsync(null, CancellationToken.None);

        Assert.Equal("Removed 0 old job records and 0 old backup files.", message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_backupDirectory))
        {
            Directory.Delete(_backupDirectory, recursive: true);
        }
    }

    private BackgroundJobMaintenanceService CreateMaintenanceService(DbContextOptions<LuminaPathDbContext> options)
    {
        var factory = new TestDbContextFactory(options);
        var settingsService = new ApplicationSettingsService(factory);
        var resolver = new BackgroundJobSettingsResolver(
            Options.Create(new BackgroundJobOptions
            {
                BackupRetentionCount = 10,
                JobHistoryRetentionDays = 30
            }),
            settingsService);
        var jobService = new BackgroundJobService(
            factory,
            new CapturingBackgroundJobQueue(),
            () => new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));
        var backupService = new DatabaseBackupService(
            new ConfigurationBuilder().Build(),
            Options.Create(new DatabaseBackupOptions { Directory = _backupDirectory }),
            new Mock<ILogger<DatabaseBackupService>>().Object);

        return new BackgroundJobMaintenanceService(resolver, jobService, backupService);
    }

    private sealed class CapturingBackgroundJobQueue : IBackgroundJobQueue
    {
        public ValueTask QueueAsync(int jobId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(0);
        }
    }
}
