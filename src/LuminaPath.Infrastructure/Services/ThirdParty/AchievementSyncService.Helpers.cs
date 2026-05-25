using System.Text.RegularExpressions;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using static LuminaPath.Core.Entities.PSN.PSNTrophy;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed partial class AchievementSyncService
{
    private static readonly Regex NonWordRegex = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

    private static Game? MatchPsnGame(IEnumerable<Game> games, TrophyTitle title)
    {
        var byCommunicationId = games.FirstOrDefault(game =>
            string.Equals(game.ExternalIds.GetExternalId(ExternalMediaProvider.Psn), title.NpCommunicationId, StringComparison.OrdinalIgnoreCase));
        if (byCommunicationId is not null)
        {
            return byCommunicationId;
        }

        var normalizedTitle = Normalize(title.TrophyTitleName);
        return games.FirstOrDefault(game =>
            Normalize(game.Name) == normalizedTitle
            || Normalize(game.Name).Replace("ps4", string.Empty).Trim() == normalizedTitle
            || Normalize(game.Name).Replace("ps5", string.Empty).Trim() == normalizedTitle);
    }

    private static GameAchievement UpsertDefinition(
        LuminaPathDbContext context,
        Game game,
        string canonicalKey,
        ExternalMediaProvider provider,
        string title,
        string? description,
        string? iconUrl,
        bool hidden,
        DateTime now,
        AchievementSyncResult result)
    {
        game.Achievements ??= new List<GameAchievement>();
        var achievement = game.Achievements.FirstOrDefault(item => item.CanonicalKey == canonicalKey);
        if (achievement is null)
        {
            achievement = new GameAchievement
            {
                Game = game,
                GameId = game.Id,
                CanonicalKey = canonicalKey,
                PrimaryProvider = provider,
                Title = title,
                Description = description,
                IconUrl = iconUrl,
                IsHidden = hidden,
                LastSyncedAt = now,
            };
            game.Achievements.Add(achievement);
            context.GameAchievements.Add(achievement);
            result.DefinitionsAdded++;
            return achievement;
        }

        achievement.Title = string.IsNullOrWhiteSpace(achievement.Title) ? title : achievement.Title;
        achievement.Description = BestText(achievement.Description, description);
        achievement.IconUrl = BestText(achievement.IconUrl, iconUrl);
        achievement.IsHidden = achievement.IsHidden && hidden;
        achievement.LastSyncedAt = now;
        result.DefinitionsUpdated++;
        return achievement;
    }

    private static void UpsertUnlock(
        GameAchievement achievement,
        string userId,
        ExternalMediaProvider provider,
        string sourceAchievementId,
        DateTime? unlockedAt,
        DateTime now,
        AchievementSyncResult result)
    {
        achievement.UserAchievements ??= new List<UserGameAchievement>();
        var existing = achievement.UserAchievements.FirstOrDefault(unlock =>
            unlock.LuminaUserId == userId
            && unlock.Provider == provider
            && unlock.SourceAchievementId.Equals(sourceAchievementId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.UnlockedAt = unlockedAt ?? existing.UnlockedAt;
            existing.SyncedAt = now;
            return;
        }

        achievement.UserAchievements.Add(new UserGameAchievement
        {
            LuminaUserId = userId,
            Provider = provider,
            SourceAchievementId = sourceAchievementId,
            UnlockedAt = unlockedAt,
            SyncedAt = now,
        });
        result.UnlocksAdded++;
    }

    private static string BuildCanonicalKey(string title, string? description, string fallback)
    {
        var normalizedTitle = Normalize(title);
        if (!string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return normalizedTitle;
        }

        return Normalize(fallback);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return NonWordRegex.Replace(value.Trim().ToLowerInvariant(), " ").Trim();
    }

    private static string PsnSourceId(int trophyId, string? groupId)
    {
        return $"{(string.IsNullOrWhiteSpace(groupId) ? "default" : groupId)}:{trophyId}";
    }

    private static string? BestText(string? current, string? next)
    {
        return string.IsNullOrWhiteSpace(current) ? next : current;
    }
}
