using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Configuration;

internal sealed class DeploymentConfigurationWarningService(
    IConfiguration config,
    IHostEnvironment environment,
    ILogger<DeploymentConfigurationWarningService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var warning in DeploymentConfigurationWarnings.Build(config, environment))
        {
            logger.LogWarning("{Warning}", warning);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class DeploymentConfigurationWarnings
{
    public static IReadOnlyList<string> Build(IConfiguration config, IHostEnvironment environment)
    {
        var warnings = new List<string>();
        var authOptions = AuthCookieOptions.FromConfiguration(config);

        if (Enum.TryParse<SameSiteMode>(authOptions.CookieSameSite, ignoreCase: true, out var sameSite))
        {
            var configuredCorsOrigins = config.GetConfiguredCorsOrigins();
            if (configuredCorsOrigins.Length > 0 && sameSite != SameSiteMode.None)
            {
                warnings.Add(
                    $"CORS has {configuredCorsOrigins.Length} configured origin(s), but Auth:CookieSameSite is '{authOptions.CookieSameSite}'. " +
                    "Cross-site browser cookie auth usually requires Auth:CookieSameSite=None and Auth:CookieSecurePolicy=Always. " +
                    "Keep Lax only when the frontend and API are same-site or the frontend proxies /api.");
            }
        }

        if (!environment.IsDevelopment() && config.GetValue("Database:RunMigrationsOnStartup", false))
        {
            warnings.Add(
                "Database:RunMigrationsOnStartup is enabled outside Development. " +
                "This is convenient for small self-hosted installs, but public production deployments should review and apply migrations deliberately.");
        }

        var storageOptions = StorageOptions.FromConfiguration(config);
        if (!environment.IsDevelopment()
            && storageOptions.UsesAzure
            && string.Equals(storageOptions.AzureBlobConnectionString, "UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "Storage:Provider is Azure, but Azure:BlobConnectionString points at development storage. " +
                "Use a real Azure Blob connection string for production deployments.");
        }

        return warnings;
    }
}
