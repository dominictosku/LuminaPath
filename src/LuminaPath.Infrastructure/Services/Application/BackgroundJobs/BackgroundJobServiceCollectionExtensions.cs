using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

internal static class BackgroundJobServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<BackgroundJobOptions>(config.GetSection(BackgroundJobOptions.SectionName));
        services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobCancellationRegistry, BackgroundJobCancellationRegistry>();
        services.AddScoped<BackgroundJobSettingsResolver>();
        services.AddScoped<BackgroundJobMaintenanceService>();
        services.AddScoped<BackgroundJobService>();
        services.AddScoped<IBackgroundJobRunner, DatabaseBackupJobRunner>();
        services.AddScoped<IBackgroundJobRunner, MaintenanceCleanupJobRunner>();
        services.AddHostedService<QueuedBackgroundJobService>();
        services.AddHostedService<BackgroundJobSchedulerService>();
        return services;
    }
}
