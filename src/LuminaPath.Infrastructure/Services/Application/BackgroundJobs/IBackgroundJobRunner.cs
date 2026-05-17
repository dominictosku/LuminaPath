namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public interface IBackgroundJobRunner
{
    string JobType { get; }

    Task<string?> RunAsync(string? payload, CancellationToken cancellationToken);
}
