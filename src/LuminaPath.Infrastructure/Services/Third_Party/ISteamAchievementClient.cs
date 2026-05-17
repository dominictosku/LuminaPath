using LuminaPath.Core.Models.Third_Party;

namespace LuminaPath.Infrastructure.Services.Third_Party;

public interface ISteamAchievementClient
{
    Task<List<SteamAchievementDefinition>> GetAchievementSchemaAsync(uint appId, CancellationToken cancellationToken);

    Task<List<SteamPlayerAchievement>> GetPlayerAchievementsAsync(string steamId, uint appId, CancellationToken cancellationToken);
}
