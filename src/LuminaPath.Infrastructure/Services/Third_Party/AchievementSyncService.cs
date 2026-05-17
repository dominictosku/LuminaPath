using System.Text.RegularExpressions;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LuminaPath.Core.Entities.PSN.PSNTrophy;

namespace LuminaPath.Infrastructure.Services.Third_Party;

public sealed class AchievementSyncService
{
    private static readonly Regex NonWordRegex = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly SteamService _steamService;
    private readonly PSNService _psnService;
    private readonly ApplicationSettingsService _settings;
    private readonly ILogger<AchievementSyncService> _logger;
    private readonly Func<DateTime> _utcNow;

    public AchievementSyncService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        SteamService steamService,
        PSNService psnService,
        ApplicationSettingsService settings,
        ILogger<AchievementSyncService> logger)
        : this(dbContextFactory, steamService, psnService, settings, logger, () => DateTime.UtcNow)
    {
    }

    public AchievementSyncService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        SteamService steamService,
        PSNService psnService,
        ApplicationSettingsService settings,
        ILogger<AchievementSyncService> logger,
        Func<DateTime> utcNow)
    {
        _dbContextFactory = dbContextFactory;
        _steamService = steamService;
        _psnService = psnService;
        _settings = settings;
        _logger = logger;
        _utcNow = utcNow;
    }

    private DateTime UtcNow => _utcNow();

    public async Task<AchievementSyncResult> SyncSteamAsync(LuminaUser user, CancellationToken cancellationToken = default)
    {
        var result = new AchievementSyncResult();
        var steamId = user.LuminaUserInfo?.SteamId;
        if (string.IsNullOrWhiteSpace(steamId))
        {
            result.Warnings.Add("Steam profile is not connected.");
            return result;
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await context.Games
            .Include(game => game.ExternalIds)
            .Include(game => game.Achievements!)
                .ThenInclude(achievement => achievement.UserAchievements)
            .Where(game => game.MyGames!.Any(myGame => myGame.LuminaUserId == user.Id))
            .ToListAsync(cancellationToken);

        foreach (var game in games)
        {
            var steamAppId = game.ExternalIds.GetExternalId(ExternalMediaProvider.Steam);
            if (!uint.TryParse(steamAppId, out var appId) || appId == 0)
            {
                continue;
            }

            result.GamesScanned++;
            var definitions = await _steamService.GetAchievementSchemaAsync(appId, cancellationToken);
            if (definitions.Count == 0)
            {
                continue;
            }

            var earned = (await _steamService.GetPlayerAchievementsAsync(steamId, appId, cancellationToken))
                .Where(achievement => achievement.Achieved)
                .ToDictionary(achievement => achievement.ApiName, StringComparer.OrdinalIgnoreCase);

            foreach (var definition in definitions)
            {
                var achievement = UpsertDefinition(
                    context,
                    game,
                    BuildCanonicalKey(definition.DisplayName, definition.Description, definition.ApiName),
                    ExternalMediaProvider.Steam,
                    definition.DisplayName,
                    definition.Description,
                    definition.IconUrl,
                    definition.Hidden,
                    UtcNow,
                    result);

                achievement.SteamApiName = definition.ApiName;
                achievement.SteamDisplayName = definition.DisplayName;

                if (earned.TryGetValue(definition.ApiName, out var playerAchievement))
                {
                    UpsertUnlock(
                        achievement,
                        user.Id,
                        ExternalMediaProvider.Steam,
                        definition.ApiName,
                        playerAchievement.UnlockTime,
                        UtcNow,
                        result);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<AchievementSyncResult> SyncPsnAsync(LuminaUser user, CancellationToken cancellationToken = default)
    {
        var result = new AchievementSyncResult();
        var accountId = user.LuminaUserInfo?.PSNAccountId;
        if (string.IsNullOrWhiteSpace(accountId))
        {
            result.Warnings.Add("PSN profile is not connected.");
            return result;
        }

        var bearerToken = await _settings.GetPsnBearerTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            result.Warnings.Add("PSN bearer token is not configured.");
            return result;
        }

        _psnService.SetBearer(bearerToken);

        var trophyTitles = await _psnService.GetUserTrophyTitles(accountId);
        if (trophyTitles.TrophyTitles.Count == 0)
        {
            result.Warnings.Add("No PSN trophy titles were returned.");
            return result;
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await context.Games
            .Include(game => game.ExternalIds)
            .Include(game => game.Achievements!)
                .ThenInclude(achievement => achievement.UserAchievements)
            .Where(game => game.MyGames!.Any(myGame => myGame.LuminaUserId == user.Id))
            .ToListAsync(cancellationToken);

        foreach (var title in trophyTitles.TrophyTitles)
        {
            var game = MatchPsnGame(games, title);
            if (game is null)
            {
                continue;
            }

            result.GamesScanned++;
            var definitions = await _psnService.GetTitleTrophies(title.NpCommunicationId, title.NpServiceName);
            if (definitions.Trophies.Count == 0)
            {
                continue;
            }

            var earned = (await _psnService.GetUserTrophiesEarnedForTitle(accountId, title.NpCommunicationId, title.NpServiceName))
                .Trophies
                .Where(trophy => trophy.Earned)
                .ToDictionary(trophy => PsnSourceId(trophy.TrophyId, trophy.TrophyGroupId), StringComparer.OrdinalIgnoreCase);

            foreach (var trophy in definitions.Trophies)
            {
                var achievement = UpsertDefinition(
                    context,
                    game,
                    BuildCanonicalKey(trophy.TrophyName, trophy.TrophyDetail, $"{trophy.TrophyGroupId}:{trophy.TrophyId}"),
                    ExternalMediaProvider.Psn,
                    trophy.TrophyName,
                    trophy.TrophyDetail,
                    trophy.TrophyIconUrl,
                    trophy.TrophyHidden,
                    UtcNow,
                    result);

                achievement.PsnTrophyId = trophy.TrophyId;
                achievement.PsnGroupId = trophy.TrophyGroupId;
                achievement.PsnTrophyType = trophy.TrophyType;

                var sourceId = PsnSourceId(trophy.TrophyId, trophy.TrophyGroupId);
                if (earned.TryGetValue(sourceId, out var userTrophy))
                {
                    UpsertUnlock(
                        achievement,
                        user.Id,
                        ExternalMediaProvider.Psn,
                        sourceId,
                        userTrophy.EarnedDateTime,
                        UtcNow,
                        result);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

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
