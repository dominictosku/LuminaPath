using LuminaPath.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class QueuedBackgroundJobService : BackgroundService
{
    private readonly IBackgroundJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedBackgroundJobService> _logger;

    public QueuedBackgroundJobService(
        IBackgroundJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<QueuedBackgroundJobService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueUnfinishedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _queue.DequeueAsync(stoppingToken);
                await ProcessAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job worker failed while waiting for work.");
            }
        }
    }

    private async Task RequeueUnfinishedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var interruptedJobs = await context.BackgroundJobs
            .Where(job => job.Status == BackgroundJobStatus.Running)
            .ToListAsync(cancellationToken);

        foreach (var job in interruptedJobs)
        {
            job.Status = BackgroundJobStatus.Canceled;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = "Application stopped before the job completed.";
        }

        var pendingJobIds = await context.BackgroundJobs
            .Where(job => job.Status == BackgroundJobStatus.Pending)
            .OrderBy(job => job.CreatedAt)
            .Select(job => job.Id)
            .ToListAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        foreach (var jobId in pendingJobIds)
        {
            await _queue.QueueAsync(jobId, cancellationToken);
        }
    }

    private async Task ProcessAsync(int jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        var cancellationRegistry = scope.ServiceProvider.GetRequiredService<IBackgroundJobCancellationRegistry>();

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.BackgroundJobs.FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Background job {JobId} was queued but no longer exists.", jobId);
            return;
        }

        if (job.Status != BackgroundJobStatus.Pending)
        {
            return;
        }

        var runners = scope.ServiceProvider.GetServices<IBackgroundJobRunner>();
        var runner = runners.FirstOrDefault(item => string.Equals(item.JobType, job.JobType, StringComparison.OrdinalIgnoreCase));
        if (runner is null)
        {
            await MarkFailedAsync(context, job, $"No runner registered for job type {job.JobType}.", cancellationToken);
            return;
        }

        job.Status = BackgroundJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        using var jobCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var registration = cancellationRegistry.Register(job.Id, jobCancellation);

        try
        {
            var message = await runner.RunAsync(job.Payload, jobCancellation.Token);
            job.Status = BackgroundJobStatus.Succeeded;
            job.CompletedAt = DateTime.UtcNow;
            job.ResultMessage = Truncate(message, 1024);
            job.ErrorMessage = null;
        }
        catch (OperationCanceledException) when (jobCancellation.IsCancellationRequested)
        {
            job.Status = BackgroundJobStatus.Canceled;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = cancellationToken.IsCancellationRequested
                ? "Application shutdown canceled the job."
                : "Canceled by administrator.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background job {JobId} failed.", job.Id);
            job.Status = BackgroundJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = Truncate(ex.Message, 4000);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task MarkFailedAsync(LuminaPathDbContext context, Core.Models.BackgroundJobRecord job, string error, CancellationToken cancellationToken)
    {
        job.Status = BackgroundJobStatus.Failed;
        job.StartedAt = DateTime.UtcNow;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorMessage = Truncate(error, 4000);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
