using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Application;

internal static class ApplicationFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationFeatureServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DatabaseBackupOptions>(config.GetSection(DatabaseBackupOptions.SectionName));
        services.AddScoped<BrowseLibraryService>();
        services.AddScoped<MediaImportService>();
        services.AddScoped<NewsAggregationService>();
        services.AddScoped<GameMetadataRefreshService>();
        services.AddScoped<DatabaseBackupService>();
        return services;
    }
}
