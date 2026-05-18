using LuminaPath.Core.Models.ThirdParty;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public interface ISteamAchievementClient
{
    Task<List<SteamAchievementDefinition>> GetAchievementSchemaAsync(uint appId, CancellationToken cancellationToken);

    Task<List<SteamPlayerAchievement>> GetPlayerAchievementsAsync(string steamId, uint appId, CancellationToken cancellationToken);
}
