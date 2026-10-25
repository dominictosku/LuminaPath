using LuminaPath.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class GoogleDriveOptions
{
    public const string SectionName = "GoogleDrive";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string FolderName { get; set; } = "LuminaPath";
    public string? FolderId { get; set; }
    public bool UploadBackups { get; set; } = true;
    public int CacheMinutes { get; set; } = 5;

    internal static void ReadSecrets(GoogleDriveOptions options, IConfiguration config)
    {
        options.ClientSecret = SecretConfiguration.GetSecret(config,
            "GoogleDrive:ClientSecret", "GoogleDrive:ClientSecretFile") ?? string.Empty;
        options.RefreshToken = SecretConfiguration.GetSecret(config,
            "GoogleDrive:RefreshToken", "GoogleDrive:RefreshTokenFile") ?? string.Empty;
    }

    internal static bool IsValid(GoogleDriveOptions options) =>
        !string.IsNullOrWhiteSpace(options.ClientId)
        && !string.IsNullOrWhiteSpace(options.ClientSecret)
        && !string.IsNullOrWhiteSpace(options.RefreshToken)
        && !string.IsNullOrWhiteSpace(options.FolderName)
        && options.CacheMinutes is >= 0 and <= 60;
}
