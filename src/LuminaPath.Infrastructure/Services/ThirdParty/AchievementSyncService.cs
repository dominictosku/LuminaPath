using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed partial class AchievementSyncService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly ISteamAchievementClient _steamAchievements;
    private readonly IPsnTrophyClient _psnTrophies;
    private readonly ApplicationSettingsService _settings;
    private readonly ILogger<AchievementSyncService> _logger;
    private readonly Func<DateTime> _utcNow;

    public AchievementSyncService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        ISteamAchievementClient steamAchievements,
        IPsnTrophyClient psnTrophies,
        ApplicationSettingsService settings,
        ILogger<AchievementSyncService> logger)
        : this(dbContextFactory, steamAchievements, psnTrophies, settings, logger, () => DateTime.UtcNow)
    {
    }

    public AchievementSyncService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        ISteamAchievementClient steamAchievements,
        IPsnTrophyClient psnTrophies,
        ApplicationSettingsService settings,
        ILogger<AchievementSyncService> logger,
        Func<DateTime> utcNow)
    {
        _dbContextFactory = dbContextFactory;
        _steamAchievements = steamAchievements;
        _psnTrophies = psnTrophies;
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
        var games = await LoadUserGamesWithAchievementsAsync(context, user.Id, cancellationToken);

        foreach (var game in games)
        {
            var steamAppId = game.ExternalIds.GetExternalId(ExternalMediaProvider.Steam);
            if (!uint.TryParse(steamAppId, out var appId) || appId == 0)
            {
                continue;
            }

            result.GamesScanned++;
            var definitions = await _steamAchievements.GetAchievementSchemaAsync(appId, cancellationToken);
            if (definitions.Count == 0)
            {
                continue;
            }

            var earned = (await _steamAchievements.GetPlayerAchievementsAsync(steamId, appId, cancellationToken))
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

        _psnTrophies.SetBearer(bearerToken);

        var trophyTitles = await _psnTrophies.GetUserTrophyTitles(accountId);
        if (trophyTitles.TrophyTitles.Count == 0)
        {
            result.Warnings.Add("No PSN trophy titles were returned.");
            return result;
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await LoadUserGamesWithAchievementsAsync(context, user.Id, cancellationToken);

        foreach (var title in trophyTitles.TrophyTitles)
        {
            var game = MatchPsnGame(games, title);
            if (game is null)
            {
                continue;
            }

            result.GamesScanned++;
            var definitions = await _psnTrophies.GetTitleTrophies(title.NpCommunicationId, title.NpServiceName);
            if (definitions.Trophies.Count == 0)
            {
                continue;
            }

            var earned = (await _psnTrophies.GetUserTrophiesEarnedForTitle(accountId, title.NpCommunicationId, title.NpServiceName))
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

    private static async Task<List<Game>> LoadUserGamesWithAchievementsAsync(
        LuminaPathDbContext context,
        string userId,
        CancellationToken cancellationToken)
    {
        return await context.Games
            .Include(game => game.ExternalIds)
            .Include(game => game.Achievements!)
                .ThenInclude(achievement => achievement.UserAchievements)
            .Where(game => game.MyGames!.Any(myGame => myGame.LuminaUserId == userId))
            .ToListAsync(cancellationToken);
    }
}
