using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Third_Party;

internal static class ThirdPartyServiceCollectionExtensions
{
    public static IServiceCollection AddThirdPartyIntegrations(this IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<PSNService>();

        services.Configure<GameNewsOptions>(config.GetSection(GameNewsOptions.SectionName));
        services.AddHttpClient<GameNewsService>();

        services.Configure<SteamOptions>(config.GetSection(SteamOptions.SectionName));
        services.AddHttpClient<SteamService>();

        return services;
    }
}
