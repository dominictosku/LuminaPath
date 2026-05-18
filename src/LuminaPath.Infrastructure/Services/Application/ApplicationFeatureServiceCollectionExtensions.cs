using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Application;

internal static class ApplicationFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationFeatureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<DatabaseBackupOptions>()
            .Bind(config.GetSection(DatabaseBackupOptions.SectionName))
            .Validate(DatabaseBackupOptions.HasRequiredPaths, "DatabaseBackup:Directory and DatabaseBackup:PgDumpPath are required.")
            .Validate(DatabaseBackupOptions.HasValidFilePrefix, "DatabaseBackup:FilePrefix is required and cannot contain invalid file-name characters.")
            .Validate(
                options => options.MaxListedBackups is >= 1 and <= 500,
                "DatabaseBackup:MaxListedBackups must be between 1 and 500.")
            .ValidateOnStart();
        services.AddScoped<BrowseLibraryService>();
        services.AddScoped<MediaImportService>();
        services.AddScoped<NewsAggregationService>();
        services.AddScoped<GameMetadataRefreshService>();
        services.AddScoped<DatabaseBackupService>();
        return services;
    }
}
