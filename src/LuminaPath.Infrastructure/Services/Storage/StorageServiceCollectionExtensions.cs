using LuminaPath.Core.Interfaces;
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
            .Validate(StorageOptions.IsSupportedProvider, "Storage:Provider must be either 'Azure' or 'FileSystem'.")
            .Validate(StorageOptions.HasValidFileSystemPath, "Storage:Path must be a valid path when Storage:Provider is 'FileSystem'.")
            .Validate(StorageOptions.HasValidAzureConfiguration, "Azure blob storage requires Azure:BlobConnectionString and a valid Azure:BlobContainerName when Storage:Provider is 'Azure'.")
            .ValidateOnStart();

        var storageOptions = StorageOptions.FromConfiguration(config);
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

        throw new InvalidOperationException("Unsupported storage provider. Set Storage:Provider to 'Azure' or 'FileSystem'.");
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
