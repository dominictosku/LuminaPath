using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services;

public class BackgroundJobServiceTests
{
    [Fact]
    public async Task EnqueueDatabaseBackupAsync_PersistsPendingJobAndQueuesId()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var queue = new CapturingBackgroundJobQueue();
        var service = new BackgroundJobService(
            new TestDbContextFactory(options),
            queue,
            () => new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var job = await service.EnqueueDatabaseBackupAsync();

        Assert.Equal(BackgroundJobTypes.DatabaseBackup, job.JobType);
        Assert.Equal("Database backup", job.DisplayName);
        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.Equal([job.Id], queue.QueuedJobIds);

        await using var context = new LuminaPathDbContext(options);
        var persisted = await context.BackgroundJobs.SingleAsync();
        Assert.Equal(job.Id, persisted.Id);
        Assert.Equal(BackgroundJobStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsNewestJobsFirst()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.BackgroundJobs.AddRange(
                new BackgroundJobRecord
                {
                    JobType = "Old",
                    DisplayName = "Old job",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc)
                },
                new BackgroundJobRecord
                {
                    JobType = "New",
                    DisplayName = "New job",
                    Status = BackgroundJobStatus.Pending,
                    CreatedAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc)
                });
            await context.SaveChangesAsync();
        }

        var service = new BackgroundJobService(new TestDbContextFactory(options), new CapturingBackgroundJobQueue());

        var history = await service.GetHistoryAsync();

        Assert.Collection(
            history,
            job => Assert.Equal("New", job.JobType),
            job => Assert.Equal("Old", job.JobType));
    }

    [Fact]
    public async Task EnqueueDatabaseBackupAsync_ReturnsExistingActiveJob()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.BackgroundJobs.Add(new BackgroundJobRecord
            {
                JobType = BackgroundJobTypes.DatabaseBackup,
                DisplayName = "Database backup",
                Status = BackgroundJobStatus.Running,
                CreatedAt = new DateTime(2026, 5, 17, 9, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync();
        }

        var queue = new CapturingBackgroundJobQueue();
        var service = new BackgroundJobService(new TestDbContextFactory(options), queue);

        var job = await service.EnqueueDatabaseBackupAsync();

        Assert.Equal(1, job.Id);
        Assert.Equal(BackgroundJobStatus.Running, job.Status);
        Assert.Empty(queue.QueuedJobIds);
    }

    [Fact]
    public async Task RetryAsync_CreatesPendingCopy_ForFailedJob()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.BackgroundJobs.Add(new BackgroundJobRecord
            {
                JobType = BackgroundJobTypes.DatabaseBackup,
                DisplayName = "Database backup",
                Status = BackgroundJobStatus.Failed,
                Payload = "{\"mode\":\"test\"}",
                CreatedAt = new DateTime(2026, 5, 17, 9, 0, 0, DateTimeKind.Utc),
                ErrorMessage = "failed"
            });
            await context.SaveChangesAsync();
        }

        var queue = new CapturingBackgroundJobQueue();
        var service = new BackgroundJobService(
            new TestDbContextFactory(options),
            queue,
            () => new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var result = await service.RetryAsync(1);
        var retry = result.Match(job => job, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.Equal(2, retry.Id);
        Assert.Equal(BackgroundJobStatus.Pending, retry.Status);
        Assert.Equal("{\"mode\":\"test\"}", retry.Payload);
        Assert.Equal([2], queue.QueuedJobIds);
    }

    [Fact]
    public async Task CancelPendingAsync_MarksJobCanceled()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.BackgroundJobs.Add(new BackgroundJobRecord
            {
                JobType = BackgroundJobTypes.DatabaseBackup,
                DisplayName = "Database backup",
                Status = BackgroundJobStatus.Pending,
                CreatedAt = new DateTime(2026, 5, 17, 9, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync();
        }

        var service = new BackgroundJobService(
            new TestDbContextFactory(options),
            new CapturingBackgroundJobQueue(),
            () => new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var result = await service.CancelPendingAsync(1);
        var canceled = result.Match(job => job, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.Equal(BackgroundJobStatus.Canceled, canceled.Status);
        Assert.Equal("Canceled by administrator.", canceled.ErrorMessage);
        Assert.Equal(new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc), canceled.CompletedAt);
    }

    [Fact]
    public async Task CancelAsync_RequestsCancellation_ForRunningJob()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.BackgroundJobs.Add(new BackgroundJobRecord
            {
                JobType = BackgroundJobTypes.DatabaseBackup,
                DisplayName = "Database backup",
                Status = BackgroundJobStatus.Running,
                CreatedAt = new DateTime(2026, 5, 17, 9, 0, 0, DateTimeKind.Utc),
                StartedAt = new DateTime(2026, 5, 17, 9, 1, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync();
        }

        var registry = new BackgroundJobCancellationRegistry();
        using var cts = new CancellationTokenSource();
        using var registration = registry.Register(1, cts);
        var service = new BackgroundJobService(
            new TestDbContextFactory(options),
            new CapturingBackgroundJobQueue(),
            registry);

        var result = await service.CancelAsync(1);
        var running = result.Match(job => job, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.Equal(BackgroundJobStatus.Running, running.Status);
        Assert.True(cts.IsCancellationRequested);
        Assert.Equal("Cancellation requested by administrator.", running.ErrorMessage);
    }

    [Fact]
    public async Task EnqueueMaintenanceCleanupAsync_PersistsPendingCleanupJob()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var queue = new CapturingBackgroundJobQueue();
        var service = new BackgroundJobService(
            new TestDbContextFactory(options),
            queue,
            () => new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var job = await service.EnqueueMaintenanceCleanupAsync();

        Assert.Equal(BackgroundJobTypes.MaintenanceCleanup, job.JobType);
        Assert.Equal("Maintenance cleanup", job.DisplayName);
        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
        Assert.Equal([job.Id], queue.QueuedJobIds);
    }

    [Fact]
    public async Task EnqueueAsync_WritesAuditLog_WhenAuditServiceIsProvided()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var factory = new TestDbContextFactory(options);
        var service = new BackgroundJobService(
            factory,
            new CapturingBackgroundJobQueue(),
            new BackgroundJobCancellationRegistry(),
            new AuditLogService(factory));

        var job = await service.EnqueueMaintenanceCleanupAsync();

        await using var context = new LuminaPathDbContext(options);
        var auditLog = await context.AuditLogs.SingleAsync();
        Assert.Equal(AuditCategories.Admin, auditLog.Category);
        Assert.Equal(AuditActions.BackgroundJobQueued, auditLog.Action);
        Assert.Equal(AuditOutcomes.Success, auditLog.Outcome);
        Assert.Equal("BackgroundJob", auditLog.TargetType);
        Assert.Equal(job.Id.ToString(), auditLog.TargetId);
        Assert.Equal("Maintenance cleanup", auditLog.TargetName);
        Assert.Contains(BackgroundJobTypes.MaintenanceCleanup, auditLog.MetadataJson);
    }

    [Fact]
    public async Task CancelAsync_WritesFailureAudit_WhenJobDoesNotExist()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var factory = new TestDbContextFactory(options);
        var service = new BackgroundJobService(
            factory,
            new CapturingBackgroundJobQueue(),
            new BackgroundJobCancellationRegistry(),
            new AuditLogService(factory));

        var result = await service.CancelAsync(404);

        Assert.True(result.IsError);
        await using var context = new LuminaPathDbContext(options);
        var auditLog = await context.AuditLogs.SingleAsync();
        Assert.Equal(AuditActions.BackgroundJobCanceled, auditLog.Action);
        Assert.Equal(AuditOutcomes.Failure, auditLog.Outcome);
        Assert.Equal("404", auditLog.TargetId);
        Assert.Equal("Background job not found.", auditLog.ErrorMessage);
    }

    [Fact]
    public async Task CleanupHistoryAsync_RemovesOnlyCompletedJobsOlderThanRetention()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.BackgroundJobs.AddRange(
                new BackgroundJobRecord
                {
                    JobType = "Old",
                    DisplayName = "Old done",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 5, 1, 10, 1, 0, DateTimeKind.Utc)
                },
                new BackgroundJobRecord
                {
                    JobType = "Recent",
                    DisplayName = "Recent done",
                    Status = BackgroundJobStatus.Succeeded,
                    CreatedAt = new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAt = new DateTime(2026, 5, 16, 10, 1, 0, DateTimeKind.Utc)
                },
                new BackgroundJobRecord
                {
                    JobType = "Pending",
                    DisplayName = "Old pending",
                    Status = BackgroundJobStatus.Pending,
                    CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)
                });
            await context.SaveChangesAsync();
        }

        var service = new BackgroundJobService(
            new TestDbContextFactory(options),
            new CapturingBackgroundJobQueue(),
            () => new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var deleted = await service.CleanupHistoryAsync(7);

        Assert.Equal(1, deleted);
        await using var assertContext = new LuminaPathDbContext(options);
        var remaining = await assertContext.BackgroundJobs.OrderBy(job => job.JobType).ToListAsync();
        Assert.Collection(
            remaining,
            job => Assert.Equal("Pending", job.JobType),
            job => Assert.Equal("Recent", job.JobType));
    }

    private sealed class CapturingBackgroundJobQueue : IBackgroundJobQueue
    {
        public List<int> QueuedJobIds { get; } = new();

        public ValueTask QueueAsync(int jobId, CancellationToken cancellationToken = default)
        {
            QueuedJobIds.Add(jobId);
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<int>(QueuedJobIds[0]);
        }
    }
}
