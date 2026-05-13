using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class BrowseLibraryService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public BrowseLibraryService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<BrowseItemDto>> GetGameReleasesAsync(string? userId, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var addedCounts = await context.MyGames
            .AsNoTracking()
            .GroupBy(myGame => myGame.GameId)
            .Select(group => new
            {
                GameId = group.Key,
                Count = group.Select(myGame => myGame.LuminaUserId).Distinct().Count()
            })
            .ToDictionaryAsync(row => row.GameId, row => row.Count, cancellationToken);

        var userEntries = string.IsNullOrWhiteSpace(userId)
            ? new Dictionary<int, MyGame>()
            : await context.MyGames
                .AsNoTracking()
                .Include(myGame => myGame.MyGameInfo)
                .Where(myGame => myGame.LuminaUserId == userId)
                .ToDictionaryAsync(myGame => myGame.GameId, cancellationToken);

        var games = await context.Games
            .AsNoTracking()
            .Include(game => game.Image)
            .Where(game => game.ReleaseDate != null && game.ParentGameId == null)
            .ToListAsync(cancellationToken);

        return games
            .Select(game => new BrowseItemDto
            {
                Id = game.Id,
                Name = game.Name,
                Description = game.Description,
                Genre = string.Join(", ", game.Genres),
                ReleaseDate = game.ReleaseDate,
                Kind = "games",
                AddedCount = addedCounts.GetValueOrDefault(game.Id),
                Image = game.Image,
                Platforms = (int)game.Platforms,
                Playtime = game.Playtime,
                LibraryEntry = userEntries.TryGetValue(game.Id, out var entry) ? MapGameEntry(entry) : null
            })
            .OrderByDescending(item => item.ReleaseDate)
            .ThenByDescending(item => item.AddedCount)
            .ToList();
    }

    public async Task<IReadOnlyList<BrowseItemDto>> GetAnimeReleasesAsync(string? userId, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var addedCounts = await context.MyAnimes
            .AsNoTracking()
            .GroupBy(myAnime => myAnime.AnimeId)
            .Select(group => new
            {
                AnimeId = group.Key,
                Count = group.Select(myAnime => myAnime.LuminaUserId).Distinct().Count()
            })
            .ToDictionaryAsync(row => row.AnimeId, row => row.Count, cancellationToken);

        var userEntries = string.IsNullOrWhiteSpace(userId)
            ? new Dictionary<int, MyAnime>()
            : await context.MyAnimes
                .AsNoTracking()
                .Where(myAnime => myAnime.LuminaUserId == userId)
                .ToDictionaryAsync(myAnime => myAnime.AnimeId, cancellationToken);

        var animes = await context.Animes
            .AsNoTracking()
            .Include(anime => anime.Image)
            .Where(anime => anime.ReleaseDate != null && anime.ParentAnimeId == null)
            .ToListAsync(cancellationToken);

        return animes
            .Select(anime => new BrowseItemDto
            {
                Id = anime.Id,
                Name = anime.Name,
                Description = anime.Description,
                Genre = string.Join(", ", anime.Genres),
                ReleaseDate = anime.ReleaseDate,
                Kind = "animes",
                AddedCount = addedCounts.GetValueOrDefault(anime.Id),
                Image = anime.Image,
                EpisodeCount = anime.EpisodeCount,
                ExpectedWatchTimePerEpisodeMinutes = anime.ExpectedWatchTimePerEpisodeMinutes,
                ExpectedWatchTimeMinutes = anime.ExpectedWatchTimeMinutes,
                LibraryEntry = userEntries.TryGetValue(anime.Id, out var entry) ? MapAnimeEntry(entry) : null
            })
            .OrderByDescending(item => item.ReleaseDate)
            .ThenByDescending(item => item.AddedCount)
            .ToList();
    }

    private static BrowseUserEntryDto MapGameEntry(MyGame entry)
        => new()
        {
            Id = entry.Id,
            Rating = entry.Rating,
            StartDate = entry.StartDate,
            EndDate = entry.EndDate,
            Status = (int)entry.Status,
            TimeSpend = entry.TimeSpend,
            MyGameInfo = entry.MyGameInfo is null
                ? null
                : new BrowseGameInfoDto
                {
                    TrackedHours = entry.MyGameInfo.TrackedHours
                }
        };

    private static BrowseUserEntryDto MapAnimeEntry(MyAnime entry)
        => new()
        {
            Id = entry.Id,
            Rating = entry.Rating,
            StartDate = entry.StartDate,
            EndDate = entry.EndDate,
            Status = (int)entry.Status,
            TimeSpend = entry.TimeSpend,
            CurrentWatchTimeMinutes = entry.CurrentWatchTimeMinutes,
            CurrentEpisode = entry.CurrentEpisode
        };
}
