using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

internal static class BackgroundJobServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<BackgroundJobOptions>()
            .Bind(config.GetSection(BackgroundJobOptions.SectionName))
            .Validate(
                BackgroundJobOptions.HasValidRanges,
                "BackgroundJobs intervals and retention values must be within supported ranges.")
            .ValidateOnStart();
        services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobCancellationRegistry, BackgroundJobCancellationRegistry>();
        services.AddScoped<BackgroundJobSettingsResolver>();
        services.AddScoped<BackgroundJobMaintenanceService>();
        services.AddScoped<BackgroundJobService>();
        services.AddScoped<OrphanedBlobCleanupService>();
        services.AddScoped<IBackgroundJobRunner, DatabaseBackupJobRunner>();
        services.AddScoped<IBackgroundJobRunner, MaintenanceCleanupJobRunner>();
        services.AddScoped<IBackgroundJobRunner, OrphanedBlobCleanupJobRunner>();
        services.AddScoped<IBackgroundJobRunner>(sp => new MetadataRefreshJobRunner(
            sp.GetRequiredService<MetadataRefreshMaintenanceService>(),
            BackgroundJobTypes.MetadataRefreshGames,
            MetadataRefreshMediaTypes.Games));
        services.AddScoped<IBackgroundJobRunner>(sp => new MetadataRefreshJobRunner(
            sp.GetRequiredService<MetadataRefreshMaintenanceService>(),
            BackgroundJobTypes.MetadataRefreshAnimes,
            MetadataRefreshMediaTypes.Animes));
        services.AddScoped<IBackgroundJobRunner>(sp => new MetadataRefreshJobRunner(
            sp.GetRequiredService<MetadataRefreshMaintenanceService>(),
            BackgroundJobTypes.MetadataRefreshMovies,
            MetadataRefreshMediaTypes.Movies));
        services.AddScoped<IBackgroundJobRunner>(sp => new MetadataRefreshJobRunner(
            sp.GetRequiredService<MetadataRefreshMaintenanceService>(),
            BackgroundJobTypes.MetadataRefreshSeries,
            MetadataRefreshMediaTypes.Series));
        services.AddHostedService<QueuedBackgroundJobService>();
        services.AddHostedService<BackgroundJobSchedulerService>();
        return services;
    }
}
