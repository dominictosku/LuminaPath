using System.Net.Http.Json;
using LuminaPath.Core.Models.ThirdParty;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed partial class SteamService
{
    public async Task<List<SteamAchievementDefinition>> GetAchievementSchemaAsync(uint appId, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || appId == 0) return new List<SteamAchievementDefinition>();

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&appid={appId}&l=english";

        try
        {
            var data = await _http.GetFromJsonAsync<SteamSchemaResponse>(url, cancellationToken);
            return data?.Game.AvailableGameStats?.Achievements
                .Where(achievement => !string.IsNullOrWhiteSpace(achievement.Name))
                .Select(achievement => new SteamAchievementDefinition
                {
                    ApiName = achievement.Name,
                    DisplayName = string.IsNullOrWhiteSpace(achievement.DisplayName) ? achievement.Name : achievement.DisplayName,
                    Description = achievement.Description,
                    IconUrl = achievement.Icon,
                    GrayIconUrl = achievement.IconGray,
                    Hidden = achievement.Hidden == 1,
                })
                .ToList() ?? new List<SteamAchievementDefinition>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam achievement schema for app {AppId}", appId);
            return new List<SteamAchievementDefinition>();
        }
    }

    public async Task<List<SteamPlayerAchievement>> GetPlayerAchievementsAsync(string steamId, uint appId, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId) || appId == 0)
        {
            return new List<SteamPlayerAchievement>();
        }

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/ISteamUserStats/GetPlayerAchievements/v1/?key={apiKey}&steamid={steamId}&appid={appId}&l=english";

        try
        {
            var data = await _http.GetFromJsonAsync<SteamPlayerAchievementsResponse>(url, cancellationToken);
            return data?.PlayerStats.Achievements
                .Where(achievement => !string.IsNullOrWhiteSpace(achievement.ApiName))
                .Select(achievement => new SteamPlayerAchievement
                {
                    ApiName = achievement.ApiName,
                    Achieved = achievement.Achieved == 1,
                    UnlockTime = achievement.UnlockTime > 0 ? UnixEpoch.AddSeconds(achievement.UnlockTime) : null,
                })
                .ToList() ?? new List<SteamPlayerAchievement>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam player achievements for {SteamId} app {AppId}", steamId, appId);
            return new List<SteamPlayerAchievement>();
        }
    }
}
