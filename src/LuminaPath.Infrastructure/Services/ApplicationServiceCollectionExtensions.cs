using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.Imports;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services;

internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddStorageServices(config);
        services.Configure<DatabaseBackupOptions>(config.GetSection(DatabaseBackupOptions.SectionName));

        services.AddScoped<GameService>();
        services.AddScoped<MyGameService>();
        services.AddScoped<AnimeService>();
        services.AddScoped<MyAnimeService>();
        services.AddScoped<MovieService>();
        services.AddScoped<MyMovieService>();
        services.AddScoped<SeriesService>();
        services.AddScoped<MySeriesService>();
        services.AddScoped<QuestService>();
        services.AddScoped<GamingSessionService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<LuminaUserService>();
        services.AddScoped<FriendsService>();
        services.AddScoped<DirectMessageService>();

        services.AddScoped<ExcelService>();
        services.AddScoped<GameImportPipeline>();
        services.AddScoped<BrowseLibraryService>();
        services.AddScoped<MediaImportService>();
        services.AddScoped<NewsAggregationService>();
        services.AddScoped<GameMetadataRefreshService>();
        services.AddScoped<DatabaseBackupService>();
        services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
        services.AddScoped<BackgroundJobService>();
        services.AddScoped<IBackgroundJobRunner, DatabaseBackupJobRunner>();
        services.AddHostedService<QueuedBackgroundJobService>();
        services.AddScoped<ApplicationSettingsService>();
        return services;
    }
}
