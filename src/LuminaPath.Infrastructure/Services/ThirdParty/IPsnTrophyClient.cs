using static LuminaPath.Core.Entities.PSN.PSNTrophy;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public interface IPsnTrophyClient
{
    void SetBearer(string token);

    Task<TrophyData> GetUserTrophyTitles(string accountId);

    Task<TitleTrophyData> GetTitleTrophies(string npCommunicationId, string? npServiceName = null);

    Task<UserTrophyData> GetUserTrophiesEarnedForTitle(string accountId, string npCommunicationId, string? npServiceName = null);
}
