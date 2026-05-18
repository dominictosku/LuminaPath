using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.ModelServices;

internal static class ModelServiceCollectionExtensions
{
    public static IServiceCollection AddModelServices(this IServiceCollection services)
    {
        services.AddScoped<GameService>();
        services.AddScoped<MyGameService>();
        services.AddScoped<AnimeService>();
        services.AddScoped<MyAnimeService>();
        services.AddScoped<MovieService>();
        services.AddScoped<MyMovieService>();
        services.AddScoped<SeriesService>();
        services.AddScoped<MySeriesService>();
        services.AddScoped<QuestService>();
        services.AddScoped<GamingSessionService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<LuminaUserService>();
        services.AddScoped<FriendsService>();
        services.AddScoped<DirectMessageService>();
        return services;
    }
}
