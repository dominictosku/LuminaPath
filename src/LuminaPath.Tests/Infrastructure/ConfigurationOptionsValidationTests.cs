using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LuminaPath.Tests.Infrastructure;

public class ConfigurationOptionsValidationTests
{
    [Fact]
    public async Task AddInfrastructure_ValidConfiguration_PassesStartupOptionsValidation()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    [Theory]
    [InlineData("DatabaseBackup:MaxListedBackups", "0", "DatabaseBackup:MaxListedBackups")]
    [InlineData("BackgroundJobs:BackupIntervalHours", "0", "BackgroundJobs intervals")]
    [InlineData("Auth:CookieSameSite", "Sideways", "Auth:CookieSameSite")]
    [InlineData("GameMetadata:Provider", "UnknownProvider", "GameMetadata:Provider")]
    [InlineData("GameNews:GoogleNewsBaseUrl", "not-a-url", "GameNews Google News")]
    [InlineData("Steam:ApiBaseUrl", "ftp://steam.example", "Steam base URLs")]
    [InlineData("PSN:ApiBaseUrl", "not-a-url", "PSN base URLs")]
    public async Task AddInfrastructure_InvalidConfiguration_FailsStartupOptionsValidation(
        string key,
        string value,
        string expectedMessage)
    {
        await using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [key] = value
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(expectedMessage, string.Join(" ", exception.Failures));
    }

    [Fact]
    public async Task AddInfrastructure_AzureStorageWithoutConnectionString_FailsStartupOptionsValidation()
    {
        await using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "Azure",
            ["Azure:BlobConnectionString"] = string.Empty
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("Azure blob storage requires", string.Join(" ", exception.Failures));
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?>? overrides = null)
    {
        var configurationValues = CreateValidConfiguration();
        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                configurationValues[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddScoped<IObjectMapper, ObjectMapper>();
        services
            .AddInfrastructure(configuration)
            .AddServer();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static Dictionary<string, string?> CreateValidConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=luminapath_test;Username=test;Password=test",
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:InstanceName"] = "LuminaPath:",
            ["Storage:Provider"] = "FileSystem",
            ["Storage:Path"] = "App_Data/storage",
            ["Azure:BlobConnectionString"] = "UseDevelopmentStorage=true",
            ["Azure:BlobContainerName"] = "media",
            ["DatabaseBackup:Directory"] = "App_Data/storage/backups",
            ["DatabaseBackup:PgDumpPath"] = "pg_dump",
            ["DatabaseBackup:FilePrefix"] = "luminapath",
            ["DatabaseBackup:MaxListedBackups"] = "20",
            ["BackgroundJobs:BackupIntervalHours"] = "24",
            ["BackgroundJobs:BackupRetentionCount"] = "10",
            ["BackgroundJobs:JobHistoryRetentionDays"] = "30",
            ["BackgroundJobs:MaintenanceIntervalHours"] = "6",
            ["Auth:CookieSameSite"] = "None",
            ["Auth:CookieSecurePolicy"] = "Always",
            ["GameNews:CacheTtlHours"] = "6",
            ["GameNews:MaxItems"] = "12",
            ["GameNews:SteamMaxLength"] = "600",
            ["GameNews:GoogleNewsBaseUrl"] = "https://news.google.com/rss/search",
            ["GameNews:GoogleNewsLocale"] = "en-US",
            ["GameNews:GoogleNewsCountry"] = "US",
            ["GameMetadata:Provider"] = "IgdbThenRawg",
            ["GameMetadata:TwitchTokenUrl"] = "https://id.twitch.tv/oauth2/token",
            ["GameMetadata:IgdbBaseUrl"] = "https://api.igdb.com/v4",
            ["GameMetadata:RawgBaseUrl"] = "https://api.rawg.io/api",
            ["Steam:ApiBaseUrl"] = "https://api.steampowered.com",
            ["Steam:StoreBaseUrl"] = "https://store.steampowered.com",
            ["PSN:AuthorizationBaseUrl"] = "https://ca.account.sony.com/api/authz/v3",
            ["PSN:ProfileBaseUrl"] = "https://us-prof.np.community.playstation.net",
            ["PSN:ApiBaseUrl"] = "https://m.np.playstation.com/api",
            ["PSN:ClientId"] = "09515159-7237-4370-9b40-3806e67c0891",
            ["PSN:RedirectUri"] = "com.scee.psxandroid.scecompcall://redirect",
            ["PSN:Scope"] = "psn:mobile.v2.core psn:clientapp",
            ["PSN:TokenAuthorizationHeader"] = "Basic test",
            ["Cors:AllowedOrigins:0"] = "http://localhost"
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "LuminaPath.Tests";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LuminaPath.Tests");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
