using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.Extensions.Options;
using LuminaPath.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Storage;

internal static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<StorageOptions>()
            .Bind(config.GetSection(StorageOptions.SectionName))
            .PostConfigure(options => StorageOptions.ApplyFallbacks(options, config))
            .Validate(StorageOptions.IsSupportedProvider, "Storage:Provider must be 'Azure', 'FileSystem' or 'GoogleDrive'.")
            .Validate(StorageOptions.HasValidFileSystemPath, "Storage:Path must be a valid path when Storage:Provider is 'FileSystem'.")
            .Validate(StorageOptions.HasValidAzureConfiguration, "Azure blob storage requires Azure:BlobConnectionString and a valid Azure:BlobContainerName when Storage:Provider is 'Azure'.")
            .ValidateOnStart();

        var storageOptions = StorageOptions.FromConfiguration(config);
        services.AddScoped<StorageMigrationService>();
        if (storageOptions.UsesGoogleDrive)
        {
            services.AddOptions<GoogleDriveOptions>()
                .Bind(config.GetSection(GoogleDriveOptions.SectionName))
                .PostConfigure(options => GoogleDriveOptions.ReadSecrets(options, config))
                .Validate(GoogleDriveOptions.IsValid, "GoogleDrive requires ClientId, ClientSecret (or ClientSecretFile), RefreshToken (or RefreshTokenFile), a FolderName and CacheMinutes between 0 and 60.")
                .ValidateOnStart();
            services.AddSingleton(sp => new GoogleDriveClient(new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                AllowAutoRedirect = false
            }), sp.GetRequiredService<IOptions<GoogleDriveOptions>>()));
            services.AddSingleton<IStorageService, GoogleDriveStorage>();
            if (config.GetValue("GoogleDrive:UploadBackups", true))
                services.AddSingleton<IBackupArchive, GoogleDriveBackupArchive>();
            return services;
        }
        if (storageOptions.UsesFileSystem)
        {
            AddFileSystemStorage(services, storageOptions);
            return services;
        }

        if (storageOptions.UsesAzure)
        {
            AddAzureStorage(services, storageOptions);
            return services;
        }

        throw new InvalidOperationException("Unsupported storage provider. Set Storage:Provider to 'Azure', 'FileSystem' or 'GoogleDrive'.");
    }

    private static void AddFileSystemStorage(IServiceCollection services, StorageOptions storageOptions)
    {
        services.AddScoped<IStorageService, FileSystemStorage>(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var fullPath = Path.IsPathRooted(storageOptions.Path)
                ? storageOptions.Path
                : Path.Combine(environment.ContentRootPath, storageOptions.Path);

            return new FileSystemStorage(fullPath, sp.GetRequiredService<ILogger<FileSystemStorage>>());
        });
    }

    private static void AddAzureStorage(IServiceCollection services, StorageOptions storageOptions)
    {
        services.AddScoped<IStorageService, AzureStorage>(sp =>
            new AzureStorage(
                storageOptions.AzureBlobConnectionString,
                storageOptions.AzureBlobContainerName,
                sp.GetRequiredService<ILogger<AzureStorage>>()));
    }
}
