using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services;

internal static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration config)
    {
        var provider = config.GetSection("Storage")["Provider"] ?? "Azure";

        if (provider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            AddFileSystemStorage(services, config);
            return services;
        }

        AddAzureStorage(services, config);
        return services;
    }

    private static void AddFileSystemStorage(IServiceCollection services, IConfiguration config)
    {
        var storagePath = config.GetSection("Storage")["Path"]
            ?? Path.Combine("App_Data", "storage");

        services.AddScoped<IStorageService, FileSystemStorage>(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var fullPath = Path.IsPathRooted(storagePath)
                ? storagePath
                : Path.Combine(environment.ContentRootPath, storagePath);

            return new FileSystemStorage(fullPath, sp.GetRequiredService<ILogger<FileSystemStorage>>());
        });
    }

    private static void AddAzureStorage(IServiceCollection services, IConfiguration config)
    {
        var connectionString = ConfigurationValues.FirstNonEmpty(
                config.GetSection("Azure")["BlobConnectionString"],
                config["AZURE_CONNECTIONSTRING"])
            ?? throw new InvalidOperationException("Missing Azure blob connection string. Set Azure:BlobConnectionString or AZURE_CONNECTIONSTRING.");

        var containerName = ConfigurationValues.FirstNonEmpty(
                config.GetSection("Azure")["BlobContainerName"],
                config["AZURE_CONTAINER_NAME"])
            ?? throw new InvalidOperationException("Missing Azure blob container name. Set Azure:BlobContainerName or AZURE_CONTAINER_NAME.");

        services.AddScoped<IStorageService, AzureStorage>(sp =>
            new AzureStorage(connectionString, containerName, sp.GetRequiredService<ILogger<AzureStorage>>()));
    }
}
