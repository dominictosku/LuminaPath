using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IBackgroundJobQueue _queue;
    private readonly Func<DateTime> _now;

    public BackgroundJobService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IBackgroundJobQueue queue)
        : this(dbContextFactory, queue, () => DateTime.UtcNow)
    {
    }

    public BackgroundJobService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IBackgroundJobQueue queue,
        Func<DateTime> now)
    {
        _dbContextFactory = dbContextFactory;
        _queue = queue;
        _now = now;
    }

    public async Task<BackgroundJobRecord> EnqueueAsync(
        string jobType,
        string displayName,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = new BackgroundJobRecord
        {
            JobType = jobType.Trim(),
            DisplayName = displayName.Trim(),
            Payload = payload,
            Status = BackgroundJobStatus.Pending,
            CreatedAt = _now()
        };

        context.BackgroundJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);
        await _queue.QueueAsync(job.Id, cancellationToken);

        return job;
    }

    public async Task<BackgroundJobRecord> EnqueueDatabaseBackupAsync(CancellationToken cancellationToken = default)
    {
        return await EnqueueAsync(
            BackgroundJobTypes.DatabaseBackup,
            "Database backup",
            cancellationToken: cancellationToken);
    }

    public async Task<List<BackgroundJobRecord>> GetHistoryAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.BackgroundJobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);
    }
}
