using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed partial class BackgroundJobService
{
    private async Task<BackgroundJobRecord> EnqueueSingletonAsync(
        string jobType,
        string displayName,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var activeJob = await context.BackgroundJobs
            .AsNoTracking()
            .Where(job => job.JobType == jobType
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
            jobType,
            displayName,
            cancellationToken: cancellationToken);
    }

    private static async Task<bool> HasActiveJobAsync(
        LuminaPathDbContext context,
        string jobType,
        CancellationToken cancellationToken)
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
