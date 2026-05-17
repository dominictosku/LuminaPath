using LuminaPath.Core.Entities.Results;
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
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var activeJob = await context.BackgroundJobs
            .AsNoTracking()
            .Where(job => job.JobType == BackgroundJobTypes.DatabaseBackup
                && (job.Status == BackgroundJobStatus.Pending || job.Status == BackgroundJobStatus.Running))
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return activeJob ?? await EnqueueAsync(
            BackgroundJobTypes.DatabaseBackup,
            "Database backup",
            cancellationToken: cancellationToken);
    }

    public async Task<BackgroundJobRecord?> GetByIdAsync(int jobId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.BackgroundJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
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

    public async Task<BackgroundJobRecord?> GetLastSuccessfulJobAsync(string jobType, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.BackgroundJobs
            .AsNoTracking()
            .Where(job => job.JobType == jobType && job.Status == BackgroundJobStatus.Succeeded)
            .OrderByDescending(job => job.CompletedAt ?? job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CleanupHistoryAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoff = _now().AddDays(-Math.Max(retentionDays, 1));
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var oldJobs = await context.BackgroundJobs
            .Where(job => job.Status != BackgroundJobStatus.Pending
                && job.Status != BackgroundJobStatus.Running
                && (job.CompletedAt ?? job.CreatedAt) < cutoff)
            .ToListAsync(cancellationToken);

        context.BackgroundJobs.RemoveRange(oldJobs);
        await context.SaveChangesAsync(cancellationToken);
        return oldJobs.Count;
    }

    public async Task<Result<BackgroundJobRecord, FailedResult>> RetryAsync(int jobId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.BackgroundJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

        if (existing is null)
        {
            return new FailedResult("Background job not found.");
        }

        if (existing.Status is not (BackgroundJobStatus.Failed or BackgroundJobStatus.Canceled))
        {
            return new FailedResult("Only failed or canceled jobs can be retried.");
        }

        if (await HasActiveJobAsync(context, existing.JobType, cancellationToken))
        {
            return new FailedResult($"A {existing.DisplayName} job is already pending or running.");
        }

        var retry = new BackgroundJobRecord
        {
            JobType = existing.JobType,
            DisplayName = existing.DisplayName,
            Payload = existing.Payload,
            Status = BackgroundJobStatus.Pending,
            CreatedAt = _now(),
            ResultMessage = $"Retry of job #{existing.Id}"
        };

        context.BackgroundJobs.Add(retry);
        await context.SaveChangesAsync(cancellationToken);
        await _queue.QueueAsync(retry.Id, cancellationToken);

        return retry;
    }

    public async Task<Result<BackgroundJobRecord, FailedResult>> CancelPendingAsync(int jobId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.BackgroundJobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        if (job is null)
        {
            return new FailedResult("Background job not found.");
        }

        if (job.Status != BackgroundJobStatus.Pending)
        {
            return new FailedResult("Only pending jobs can be canceled.");
        }

        job.Status = BackgroundJobStatus.Canceled;
        job.CompletedAt = _now();
        job.ErrorMessage = "Canceled by administrator.";
        await context.SaveChangesAsync(cancellationToken);

        return job;
    }

    private static async Task<bool> HasActiveJobAsync(LuminaPathDbContext context, string jobType, CancellationToken cancellationToken)
    {
        return await context.BackgroundJobs.AnyAsync(job => job.JobType == jobType
            && (job.Status == BackgroundJobStatus.Pending || job.Status == BackgroundJobStatus.Running), cancellationToken);
    }
}
