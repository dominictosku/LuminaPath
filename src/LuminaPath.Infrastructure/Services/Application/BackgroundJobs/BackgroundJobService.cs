using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IBackgroundJobQueue _queue;
    private readonly IBackgroundJobCancellationRegistry _cancellationRegistry;
    private readonly Func<DateTime> _now;
    private readonly AuditLogService? _auditLog;

    public BackgroundJobService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IBackgroundJobQueue queue)
        : this(dbContextFactory, queue, new BackgroundJobCancellationRegistry(), () => DateTime.UtcNow)
    {
    }

    public BackgroundJobService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IBackgroundJobQueue queue,
        IBackgroundJobCancellationRegistry cancellationRegistry)
        : this(dbContextFactory, queue, cancellationRegistry, () => DateTime.UtcNow)
    {
    }

    [ActivatorUtilitiesConstructor]
    public BackgroundJobService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IBackgroundJobQueue queue,
        IBackgroundJobCancellationRegistry cancellationRegistry,
        AuditLogService auditLog)
        : this(dbContextFactory, queue, cancellationRegistry, () => DateTime.UtcNow, auditLog)
    {
    }

    public BackgroundJobService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IBackgroundJobQueue queue,
        IBackgroundJobCancellationRegistry cancellationRegistry,
        Func<DateTime> now)
        : this(dbContextFactory, queue, cancellationRegistry, now, null)
    {
    }

    private BackgroundJobService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IBackgroundJobQueue queue,
        IBackgroundJobCancellationRegistry cancellationRegistry,
        Func<DateTime> now,
        AuditLogService? auditLog)
    {
        _dbContextFactory = dbContextFactory;
        _queue = queue;
        _cancellationRegistry = cancellationRegistry;
        _now = now;
        _auditLog = auditLog;
    }

    public BackgroundJobService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IBackgroundJobQueue queue,
        Func<DateTime> now)
        : this(dbContextFactory, queue, new BackgroundJobCancellationRegistry(), now)
    {
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
        await AuditJobAsync(AuditActions.BackgroundJobQueued, AuditOutcomes.Success, job, cancellationToken: cancellationToken);

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

        if (activeJob is not null)
        {
            await AuditJobAsync(
                AuditActions.BackgroundJobQueued,
                AuditOutcomes.Success,
                activeJob,
                new { reusedExisting = true },
                cancellationToken);
            return activeJob;
        }

        return await EnqueueAsync(
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
            await AuditJobAsync(
                AuditActions.BackgroundJobRetried,
                AuditOutcomes.Failure,
                jobId: jobId,
                errorMessage: "Background job not found.",
                cancellationToken: cancellationToken);
            return new FailedResult("Background job not found.");
        }

        if (existing.Status is not (BackgroundJobStatus.Failed or BackgroundJobStatus.Canceled))
        {
            await AuditJobAsync(
                AuditActions.BackgroundJobRetried,
                AuditOutcomes.Failure,
                existing,
                errorMessage: "Only failed or canceled jobs can be retried.",
                cancellationToken: cancellationToken);
            return new FailedResult("Only failed or canceled jobs can be retried.");
        }

        if (await HasActiveJobAsync(context, existing.JobType, cancellationToken))
        {
            await AuditJobAsync(
                AuditActions.BackgroundJobRetried,
                AuditOutcomes.Failure,
                existing,
                errorMessage: $"A {existing.DisplayName} job is already pending or running.",
                cancellationToken: cancellationToken);
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
        await AuditJobAsync(
            AuditActions.BackgroundJobRetried,
            AuditOutcomes.Success,
            retry,
            new { retryOfJobId = existing.Id },
            cancellationToken);

        return retry;
    }

    public async Task<Result<BackgroundJobRecord, FailedResult>> CancelAsync(int jobId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.BackgroundJobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        if (job is null)
        {
            await AuditJobAsync(
                AuditActions.BackgroundJobCanceled,
                AuditOutcomes.Failure,
                jobId: jobId,
                errorMessage: "Background job not found.",
                cancellationToken: cancellationToken);
            return new FailedResult("Background job not found.");
        }

        if (job.Status == BackgroundJobStatus.Pending)
        {
            job.Status = BackgroundJobStatus.Canceled;
            job.CompletedAt = _now();
            job.ErrorMessage = "Canceled by administrator.";
            await context.SaveChangesAsync(cancellationToken);
            await AuditJobAsync(AuditActions.BackgroundJobCanceled, AuditOutcomes.Success, job, cancellationToken: cancellationToken);

            return job;
        }

        if (job.Status == BackgroundJobStatus.Running)
        {
            if (!_cancellationRegistry.RequestCancellation(job.Id))
            {
                await AuditJobAsync(
                    AuditActions.BackgroundJobCanceled,
                    AuditOutcomes.Failure,
                    job,
                    errorMessage: "Running job could not be canceled because it is not active on this app instance.",
                    cancellationToken: cancellationToken);
                return new FailedResult("Running job could not be canceled because it is not active on this app instance.");
            }

            job.ErrorMessage = "Cancellation requested by administrator.";
            await context.SaveChangesAsync(cancellationToken);
            await AuditJobAsync(
                AuditActions.BackgroundJobCanceled,
                AuditOutcomes.Success,
                job,
                new { cancellationRequested = true },
                cancellationToken);

            return job;
        }

        await AuditJobAsync(
            AuditActions.BackgroundJobCanceled,
            AuditOutcomes.Failure,
            job,
            errorMessage: "Only pending or running jobs can be canceled.",
            cancellationToken: cancellationToken);
        return new FailedResult("Only pending or running jobs can be canceled.");
    }

    public Task<Result<BackgroundJobRecord, FailedResult>> CancelPendingAsync(int jobId, CancellationToken cancellationToken = default)
    {
        return CancelAsync(jobId, cancellationToken);
    }

    public async Task<BackgroundJobRecord> EnqueueMaintenanceCleanupAsync(CancellationToken cancellationToken = default)
    {
        return await EnqueueAsync(
            BackgroundJobTypes.MaintenanceCleanup,
            "Maintenance cleanup",
            cancellationToken: cancellationToken);
    }

    private static async Task<bool> HasActiveJobAsync(LuminaPathDbContext context, string jobType, CancellationToken cancellationToken)
    {
        return await context.BackgroundJobs.AnyAsync(job => job.JobType == jobType
            && (job.Status == BackgroundJobStatus.Pending || job.Status == BackgroundJobStatus.Running), cancellationToken);
    }

    private Task AuditJobAsync(
        string action,
        string outcome,
        BackgroundJobRecord? job = null,
        object? metadata = null,
        CancellationToken cancellationToken = default,
        int? jobId = null,
        string? errorMessage = null)
    {
        if (_auditLog is null)
        {
            return Task.CompletedTask;
        }

        return _auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Admin,
            Action = action,
            Outcome = outcome,
            TargetType = "BackgroundJob",
            TargetId = (job?.Id ?? jobId)?.ToString(),
            TargetName = job?.DisplayName,
            Metadata = metadata ?? new
            {
                jobType = job?.JobType,
                status = job?.Status.ToString()
            },
            ErrorMessage = errorMessage
        }, cancellationToken);
    }
}
