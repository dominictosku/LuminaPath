using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LuminaPath.Infrastructure.Configuration;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

internal static class ThirdPartyServiceCollectionExtensions
{
    public static IServiceCollection AddThirdPartyIntegrations(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<PsnOptions>()
            .Bind(config.GetSection(PsnOptions.SectionName))
            .Validate(PsnOptions.HasValidUrls, "PSN base URLs must be absolute HTTP or HTTPS URLs.")
            .Validate(PsnOptions.HasRequiredOAuthSettings, "PSN OAuth settings are incomplete.")
            .ValidateOnStart();
        services.AddHttpClient<PSNService>();
        services.AddScoped<IPsnTrophyClient>(sp => sp.GetRequiredService<PSNService>());

        services.AddOptions<GameNewsOptions>()
            .Bind(config.GetSection(GameNewsOptions.SectionName))
            .Validate(GameNewsOptions.HasValidRanges, "GameNews numeric options are outside the supported ranges.")
            .Validate(GameNewsOptions.HasValidGoogleNewsSettings, "GameNews Google News settings are incomplete or invalid.")
            .ValidateOnStart();
        services.AddHttpClient<GameNewsService>();

        services.AddOptions<GameMetadataOptions>()
            .Bind(config.GetSection(GameMetadataOptions.SectionName))
            .PostConfigure(options =>
            {
                options.IgdbClientId.UseEnvironmentFallback(value => options.IgdbClientId = value, "IGDB_CLIENT_ID");
                options.IgdbClientSecret.UseEnvironmentFallback(value => options.IgdbClientSecret = value, "IGDB_CLIENT_SECRET");
                options.RawgApiKey.UseEnvironmentFallback(value => options.RawgApiKey = value, "RAWG_API_KEY");
            })
            .Validate(GameMetadataOptions.HasValidProvider, "GameMetadata:Provider is not supported.")
            .Validate(GameMetadataOptions.HasValidUrls, "GameMetadata URLs must be absolute HTTP or HTTPS URLs.")
            .Validate(GameMetadataOptions.HasCompleteOptionalCredentials, "GameMetadata IGDB credentials must include both client id and client secret when configured.")
            .ValidateOnStart();
        services.AddHttpClient<IgdbMetadataProvider>();
        services.AddHttpClient<RawgMetadataProvider>();
        services.AddScoped<IGameMetadataProvider>(sp => sp.GetRequiredService<IgdbMetadataProvider>());
        services.AddScoped<IGameMetadataProvider>(sp => sp.GetRequiredService<RawgMetadataProvider>());

        services.AddOptions<SteamOptions>()
            .Bind(config.GetSection(SteamOptions.SectionName))
            .Validate(SteamOptions.HasValidUrls, "Steam base URLs must be absolute HTTP or HTTPS URLs.")
            .ValidateOnStart();
        services.AddHttpClient<SteamService>();
        services.AddScoped<ISteamAchievementClient>(sp => sp.GetRequiredService<SteamService>());
        services.AddScoped<AchievementSyncService>();

        return services;
    }
}
