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
            .Validate<IEnumerable<StorageProviderRegistration>>((options, registrations) => registrations.Any(
                provider => provider.Name.Equals(options.Provider, StringComparison.OrdinalIgnoreCase)),
                "Storage:Provider must name a registered storage provider.")
            .Validate(StorageOptions.HasValidFileSystemPath, "Storage:Path must be a valid path when Storage:Provider is 'FileSystem'.")
            .Validate(StorageOptions.HasValidAzureConfiguration, "Azure blob storage requires Azure:BlobConnectionString and a valid Azure:BlobContainerName when Storage:Provider is 'Azure'.")
            .ValidateOnStart();

        var storageOptions = StorageOptions.FromConfiguration(config);
        services.AddScoped<StorageMigrationService>();
        services.AddScoped<StorageReconciliationService>();
        services.AddOptions<GoogleDriveOptions>()
            .Bind(config.GetSection(GoogleDriveOptions.SectionName))
            .PostConfigure(options => GoogleDriveOptions.ReadSecrets(options, config))
            .Validate(options => (!storageOptions.UsesGoogleDrive && string.IsNullOrWhiteSpace(options.ClientId)
                && string.IsNullOrWhiteSpace(options.ClientSecret)) || GoogleDriveOptions.IsValid(options),
                "GoogleDrive requires ClientId, ClientSecret (or ClientSecretFile), a FolderName, ConnectionPath, CacheMinutes between 0 and 60, and a valid RedirectUri when supplied.")
            .ValidateOnStart();
        services.AddSingleton(sp => new GoogleDriveConnectionStore(
            sp.GetRequiredService<IOptions<GoogleDriveOptions>>(), sp.GetRequiredService<IHostEnvironment>()));
        services.AddSingleton(sp => new GoogleDriveConnectionService(CreateDriveHttpClient(),
            sp.GetRequiredService<IOptions<GoogleDriveOptions>>(), sp.GetRequiredService<GoogleDriveConnectionStore>()));
        services.AddSingleton(sp => new GoogleDriveClient(CreateDriveHttpClient(),
            sp.GetRequiredService<IOptions<GoogleDriveOptions>>(), sp.GetRequiredService<GoogleDriveConnectionStore>()));
        services.AddStorageProvider(StorageOptions.GoogleDriveProvider, sp =>
        {
            var options = sp.GetRequiredService<IOptions<GoogleDriveOptions>>();
            if (!GoogleDriveOptions.IsValid(options.Value))
                throw new InvalidOperationException("Configure GoogleDrive credentials before using this storage provider.");
            return new GoogleDriveStorage(sp.GetRequiredService<GoogleDriveClient>(), options,
                sp.GetRequiredService<ILogger<GoogleDriveStorage>>());
        });
        services.AddStorageProvider(StorageOptions.FileSystemProvider, sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var fullPath = Path.GetFullPath(storageOptions.Path, environment.ContentRootPath);
            return new FileSystemStorage(fullPath, sp.GetRequiredService<ILogger<FileSystemStorage>>());
        });
        services.AddStorageProvider(StorageOptions.AzureProvider, sp =>
        {
            if (!StorageOptions.HasValidAzureConfiguration(new StorageOptions
            {
                Provider = StorageOptions.AzureProvider,
                AzureBlobConnectionString = storageOptions.AzureBlobConnectionString,
                AzureBlobContainerName = storageOptions.AzureBlobContainerName
            })) throw new InvalidOperationException("Configure Azure blob storage credentials before using this provider.");
            return new AzureStorage(storageOptions.AzureBlobConnectionString, storageOptions.AzureBlobContainerName,
                sp.GetRequiredService<ILogger<AzureStorage>>());
        });
        services.AddOptions<S3Options>()
            .Bind(config.GetSection(S3Options.SectionName))
            .PostConfigure(options => S3Options.ReadSecrets(options, config))
            .Validate(options => (!storageOptions.Provider.Equals(StorageOptions.S3Provider, StringComparison.OrdinalIgnoreCase)
                && !config.GetSection(S3Options.SectionName).Exists()) || S3Options.IsValid(options),
                "S3 requires BucketName, Region, a valid endpoint/prefix and either a complete credential pair or the AWS credential chain.")
            .ValidateOnStart();
        services.AddSingleton<Amazon.S3.IAmazonS3>(sp => sp.GetRequiredService<IOptions<S3Options>>().Value.CreateClient());
        services.AddSingleton<S3Storage>();
        services.AddStorageProvider(StorageOptions.S3Provider, sp => sp.GetRequiredService<S3Storage>());
        if (storageOptions.Provider.Equals(StorageOptions.S3Provider, StringComparison.OrdinalIgnoreCase)
            && config.GetValue("S3:UploadBackups", true))
            services.AddSingleton<IBackupArchive, S3BackupArchive>();
        services.AddSingleton<StorageProviderRegistry>();
        services.AddSingleton<IStorageService>(sp =>
        {
            var registry = sp.GetRequiredService<StorageProviderRegistry>();
            var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var primary = registry.Get(options.Provider);
            IStorageService? fallback = null;
            if (!string.IsNullOrWhiteSpace(options.FallbackProvider)
                && !string.Equals(options.Provider, options.FallbackProvider, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    fallback = registry.Get(options.FallbackProvider);
                }
                catch (Exception ex)
                {
                    sp.GetRequiredService<ILogger<FallbackStorageService>>()
                        .LogWarning(ex, "Configured FallbackProvider '{Fallback}' could not be resolved.", options.FallbackProvider);
                }
            }

            return fallback is not null
                ? new FallbackStorageService(primary, fallback, sp.GetRequiredService<ILogger<FallbackStorageService>>())
                : primary;
        });
        if (storageOptions.UsesGoogleDrive && config.GetValue("GoogleDrive:UploadBackups", true))
            services.AddSingleton<IBackupArchive, GoogleDriveBackupArchive>();
        return services;
    }

    // Avoid logging OAuth tokens or resumable-session URLs through HttpClientFactory.
    private static HttpClient CreateDriveHttpClient() => new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2), AllowAutoRedirect = false
    });
}
