using System.Text.RegularExpressions;
using LuminaPath.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed partial class StorageOptions
{
    public const string SectionName = "Storage";
    public const string AzureProvider = "Azure";
    public const string FileSystemProvider = "FileSystem";

    public string Provider { get; set; } = FileSystemProvider;
    public string Path { get; set; } = System.IO.Path.Combine("App_Data", "storage");
    public string AzureBlobConnectionString { get; set; } = string.Empty;
    public string AzureBlobContainerName { get; set; } = "media";

    public bool UsesFileSystem => Provider.Equals(FileSystemProvider, StringComparison.OrdinalIgnoreCase);
    public bool UsesAzure => Provider.Equals(AzureProvider, StringComparison.OrdinalIgnoreCase);

    internal static StorageOptions FromConfiguration(IConfiguration config)
    {
        var options = new StorageOptions();
        config.GetSection(SectionName).Bind(options);
        ApplyFallbacks(options, config);
        return options;
    }

    internal static void ApplyFallbacks(StorageOptions options, IConfiguration config)
    {
        options.Provider = ConfigurationValues.FirstNonEmpty(
                options.Provider,
                FileSystemProvider)
            ?? FileSystemProvider;

        options.Path = ConfigurationValues.FirstNonEmpty(
                options.Path,
                System.IO.Path.Combine("App_Data", "storage"))
            ?? System.IO.Path.Combine("App_Data", "storage");

        options.AzureBlobConnectionString = ConfigurationValues.FirstNonEmpty(
                config.GetSection(SectionName)["AzureBlobConnectionString"],
                config.GetSection("Azure")["BlobConnectionString"],
                options.AzureBlobConnectionString)
            ?? string.Empty;

        options.AzureBlobContainerName = ConfigurationValues.FirstNonEmpty(
                config.GetSection(SectionName)["AzureBlobContainerName"],
                config.GetSection("Azure")["BlobContainerName"],
                options.AzureBlobContainerName,
                "media")
            ?? "media";
    }

    internal static bool IsSupportedProvider(StorageOptions options)
    {
        return options.UsesAzure || options.UsesFileSystem;
    }

    internal static bool HasValidFileSystemPath(StorageOptions options)
    {
        if (!options.UsesFileSystem)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.Path))
        {
            return false;
        }

        try
        {
            _ = System.IO.Path.GetFullPath(options.Path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool HasValidAzureConfiguration(StorageOptions options)
    {
        return !options.UsesAzure
            || !string.IsNullOrWhiteSpace(options.AzureBlobConnectionString)
            && IsValidAzureContainerName(options.AzureBlobContainerName);
    }

    private static bool IsValidAzureContainerName(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && AzureContainerNameRegex().IsMatch(value);
    }

    [GeneratedRegex("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$", RegexOptions.Compiled)]
    private static partial Regex AzureContainerNameRegex();
}
