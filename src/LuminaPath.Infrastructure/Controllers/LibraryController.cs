using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LibraryController : ControllerBase
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

        public LibraryController(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        [HttpGet("games/releases")]
        public async Task<ActionResult<IReadOnlyList<ReleaseLibraryItemDto>>> GetGameReleases(CancellationToken cancellationToken)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var userId = CurrentUserId();

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
                .Where(game => game.ReleaseDate != null)
                .ToListAsync(cancellationToken);

            return Ok(games
                .Select(game => new ReleaseLibraryItemDto
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
                .ToList());
        }

        [HttpGet("animes/releases")]
        public async Task<ActionResult<IReadOnlyList<ReleaseLibraryItemDto>>> GetAnimeReleases(CancellationToken cancellationToken)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var userId = CurrentUserId();

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
                .Where(anime => anime.ReleaseDate != null)
                .ToListAsync(cancellationToken);

            return Ok(animes
                .Select(anime => new ReleaseLibraryItemDto
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
                .ToList());
        }

        private string? CurrentUserId()
            => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private static ReleaseLibraryUserEntryDto MapGameEntry(MyGame entry)
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
                    : new ReleaseLibraryGameInfoDto
                    {
                        TrackedHours = entry.MyGameInfo.TrackedHours
                    }
            };

        private static ReleaseLibraryUserEntryDto MapAnimeEntry(MyAnime entry)
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
}
