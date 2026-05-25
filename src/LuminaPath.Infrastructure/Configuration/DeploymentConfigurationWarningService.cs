using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
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
        var summary = DeploymentConfigurationSummary.Build(config, environment);
        logger.LogInformation(
            "LuminaPath configuration: Environment={Environment}; DatabaseConfigured={DatabaseConfigured}; RedisConfigured={RedisConfigured}; StorageProvider={StorageProvider}; RunMigrationsOnStartup={RunMigrationsOnStartup}; CorsOriginCount={CorsOriginCount}; CookieSameSite={CookieSameSite}; CookieSecurePolicy={CookieSecurePolicy}; RequireAdminApproval={RequireAdminApproval}; AiProvider={AiProvider}; ScheduledJobsEnabled={ScheduledJobsEnabled}",
            summary.Environment,
            summary.DatabaseConfigured,
            summary.RedisConfigured,
            summary.StorageProvider,
            summary.RunMigrationsOnStartup,
            summary.CorsOriginCount,
            summary.CookieSameSite,
            summary.CookieSecurePolicy,
            summary.RequireAdminApproval,
            summary.AiProvider,
            summary.ScheduledJobsEnabled);

        foreach (var warning in DeploymentConfigurationWarnings.Build(config, environment))
        {
            logger.LogWarning("{Warning}", warning);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed record DeploymentConfigurationSummary(
    string Environment,
    bool DatabaseConfigured,
    bool RedisConfigured,
    string StorageProvider,
    bool RunMigrationsOnStartup,
    int CorsOriginCount,
    string CookieSameSite,
    string CookieSecurePolicy,
    bool RequireAdminApproval,
    string AiProvider,
    bool ScheduledJobsEnabled)
{
    public static DeploymentConfigurationSummary Build(IConfiguration config, IHostEnvironment environment)
    {
        var authCookieOptions = AuthCookieOptions.FromConfiguration(config);
        var authRegistrationOptions = AuthRegistrationOptions.FromConfiguration(config);
        var storageOptions = StorageOptions.FromConfiguration(config);
        var redisOptions = RedisOptions.FromConfiguration(config);

        return new DeploymentConfigurationSummary(
            Environment: environment.EnvironmentName,
            DatabaseConfigured: !string.IsNullOrWhiteSpace(
                ConfigurationValues.FirstNonEmpty(config.GetConnectionString("Default"), config["POSTGRESQL_DB"])),
            RedisConfigured: !string.IsNullOrWhiteSpace(redisOptions.ConnectionString),
            StorageProvider: storageOptions.Provider,
            RunMigrationsOnStartup: config.GetValue("Database:RunMigrationsOnStartup", false),
            CorsOriginCount: config.GetConfiguredCorsOrigins().Length,
            CookieSameSite: authCookieOptions.CookieSameSite,
            CookieSecurePolicy: authCookieOptions.CookieSecurePolicy,
            RequireAdminApproval: authRegistrationOptions.RequireAdminApproval,
            AiProvider: config[AiChatOptions.SectionName + ":Provider"] ?? new AiChatOptions().Provider,
            ScheduledJobsEnabled: config.GetValue("BackgroundJobs:ScheduledJobsEnabled", new BackgroundJobOptions().ScheduledJobsEnabled));
    }
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
