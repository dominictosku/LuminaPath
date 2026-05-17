using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
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
