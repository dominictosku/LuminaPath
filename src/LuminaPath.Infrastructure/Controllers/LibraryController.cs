using LuminaPath.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

            var addedCounts = await context.MyGames
                .AsNoTracking()
                .GroupBy(myGame => myGame.GameId)
                .Select(group => new
                {
                    GameId = group.Key,
                    Count = group.Select(myGame => myGame.LuminaUserId).Distinct().Count()
                })
                .ToDictionaryAsync(row => row.GameId, row => row.Count, cancellationToken);

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
                    Playtime = game.Playtime
                })
                .OrderByDescending(item => item.ReleaseDate)
                .ThenByDescending(item => item.AddedCount)
                .ToList());
        }

        [HttpGet("animes/releases")]
        public async Task<ActionResult<IReadOnlyList<ReleaseLibraryItemDto>>> GetAnimeReleases(CancellationToken cancellationToken)
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
                    ExpectedWatchTimeMinutes = anime.ExpectedWatchTimeMinutes
                })
                .OrderByDescending(item => item.ReleaseDate)
                .ThenByDescending(item => item.AddedCount)
                .ToList());
        }
    }
}
