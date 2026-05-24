using LuminaPath.Core.Dtos.Statistics;
using LuminaPath.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices.Statistics;

/// <summary>
/// Per-user aggregate queries for the statistics page. Currently just PSN
/// trophy totals — kept as its own service so the file doesn't accrete into
/// the per-media services and so future "stat panel" endpoints have a clear
/// home (cumulative playtime by year, achievement rate, etc.).
/// </summary>
public sealed class StatisticsService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public StatisticsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Counts the user's unlocked PSN trophies grouped by tier. A trophy
    /// counts as "unlocked" when its <see cref="Core.Models.UserGameAchievement.UnlockedAt"/>
    /// is non-null AND its parent <see cref="Core.Models.GameAchievement.PsnTrophyType"/>
    /// is one of {bronze, silver, gold, platinum}. Anything else (PSN's
    /// `definitions`-only rows, trophies missing a type from the API) is
    /// skipped so the totals strictly match what PSN shows the user.
    /// </summary>
    public async Task<PsnTrophyTotalsDto> GetPsnTrophyTotalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // GROUP BY runs on the database. Returns at most 4 rows (one per
        // tier), so the materialised dictionary is tiny.
        var perTier = await dbContext.UserGameAchievements
            .AsNoTracking()
            .Where(unlock =>
                unlock.LuminaUserId == userId
                && unlock.Provider == ExternalMediaProvider.Psn
                && unlock.UnlockedAt != null
                && unlock.GameAchievement != null
                && unlock.GameAchievement.PsnTrophyType != null)
            .GroupBy(unlock => unlock.GameAchievement!.PsnTrophyType!.ToLower())
            .Select(group => new { Tier = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Tier, row => row.Count, cancellationToken);

        var bronze = perTier.GetValueOrDefault("bronze");
        var silver = perTier.GetValueOrDefault("silver");
        var gold = perTier.GetValueOrDefault("gold");
        var platinum = perTier.GetValueOrDefault("platinum");

        return new PsnTrophyTotalsDto(
            Bronze: bronze,
            Silver: silver,
            Gold: gold,
            Platinum: platinum,
            Total: bronze + silver + gold + platinum);
    }
}
