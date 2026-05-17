using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Third_Party;

internal static class ThirdPartyServiceCollectionExtensions
{
    public static IServiceCollection AddThirdPartyIntegrations(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<PSNService>();
        services.AddScoped<IPsnTrophyClient>(sp => sp.GetRequiredService<PSNService>());

        services.Configure<GameNewsOptions>(config.GetSection(GameNewsOptions.SectionName));
        services.AddHttpClient<GameNewsService>();

        services.Configure<GameMetadataOptions>(config.GetSection(GameMetadataOptions.SectionName));
        services.AddHttpClient<IgdbMetadataProvider>();
        services.AddHttpClient<RawgMetadataProvider>();
        services.AddScoped<IGameMetadataProvider>(sp => sp.GetRequiredService<IgdbMetadataProvider>());
        services.AddScoped<IGameMetadataProvider>(sp => sp.GetRequiredService<RawgMetadataProvider>());

        services.Configure<SteamOptions>(config.GetSection(SteamOptions.SectionName));
        services.AddHttpClient<SteamService>();
        services.AddScoped<ISteamAchievementClient>(sp => sp.GetRequiredService<SteamService>());
        services.AddScoped<AchievementSyncService>();

        return services;
    }
}
