using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public sealed class GlobalSearchService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public GlobalSearchService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<GlobalSearchResultDto>> SearchAsync(
        string userId,
        string? query,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        if (normalizedQuery.Length < 2)
        {
            return [];
        }

        limit = Math.Clamp(limit, 1, 25);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var hits = new List<SearchHit>();

        await AddGameHits(dbContext, userId, normalizedQuery, hits, cancellationToken);
        await AddAnimeHits(dbContext, userId, normalizedQuery, hits, cancellationToken);
        await AddMovieHits(dbContext, userId, normalizedQuery, hits, cancellationToken);
        await AddSeriesHits(dbContext, userId, normalizedQuery, hits, cancellationToken);
        await AddQuestHits(dbContext, userId, normalizedQuery, hits, cancellationToken);

        return hits
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Result.Title)
            .Take(limit)
            .Select(hit => hit.Result)
            .ToList();
    }

    private static async Task AddGameHits(
        LuminaPathDbContext dbContext,
        string userId,
        string query,
        List<SearchHit> hits,
        CancellationToken cancellationToken)
    {
        var games = await dbContext.MyGames
            .AsNoTracking()
            .Include(myGame => myGame.Game)
            .Where(myGame => myGame.LuminaUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var myGame in games)
        {
            var game = myGame.Game;
            var title = game?.Name ?? "Untitled game";
            var score = Score(query, title, myGame.PersonalNotes, game?.Description, GenresText(game));
            if (score == 0)
            {
                continue;
            }

            var noteMatch = Matches(myGame.PersonalNotes, query);
            hits.Add(new SearchHit(
                score + (noteMatch ? 10 : 0),
                new GlobalSearchResultDto
                {
                    Kind = noteMatch ? "note" : "library",
                    Title = title,
                    Subtitle = $"{myGame.Status} game",
                    MatchedText = FirstSnippet(query,
                        ("Notes", myGame.PersonalNotes),
                        ("Description", game?.Description),
                        ("Genres", GenresText(game))),
                    Route = $"/library/games/{myGame.GameId}",
                    Icon = noteMatch ? "document-text-outline" : "game-controller-outline",
                    MediaKind = "games",
                    MediaId = myGame.GameId,
                    LibraryEntryId = myGame.Id
                }));
        }
    }

    private static async Task AddAnimeHits(
        LuminaPathDbContext dbContext,
        string userId,
        string query,
        List<SearchHit> hits,
        CancellationToken cancellationToken)
    {
        var animes = await dbContext.MyAnimes
            .AsNoTracking()
            .Include(myAnime => myAnime.Anime)
            .Where(myAnime => myAnime.LuminaUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var myAnime in animes)
        {
            var anime = myAnime.Anime;
            var title = anime?.Name ?? "Untitled anime";
            var score = Score(query, title, anime?.Description, GenresText(anime));
            if (score == 0)
            {
                continue;
            }

            hits.Add(new SearchHit(
                score,
                new GlobalSearchResultDto
                {
                    Kind = "library",
                    Title = title,
                    Subtitle = $"{myAnime.Status} anime",
                    MatchedText = FirstSnippet(query,
                        ("Description", anime?.Description),
                        ("Genres", GenresText(anime))),
                    Route = $"/library/animes/{myAnime.AnimeId}",
                    Icon = "sparkles-outline",
                    MediaKind = "animes",
                    MediaId = myAnime.AnimeId,
                    LibraryEntryId = myAnime.Id
                }));
        }
    }

    private static async Task AddMovieHits(
        LuminaPathDbContext dbContext,
        string userId,
        string query,
        List<SearchHit> hits,
        CancellationToken cancellationToken)
    {
        var movies = await dbContext.MyMovies
            .AsNoTracking()
            .Include(myMovie => myMovie.Movie)
            .Where(myMovie => myMovie.LuminaUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var myMovie in movies)
        {
            var movie = myMovie.Movie;
            var title = movie?.Name ?? "Untitled movie";
            var score = Score(query, title, movie?.Description, GenresText(movie));
            if (score == 0)
            {
                continue;
            }

            hits.Add(new SearchHit(
                score,
                new GlobalSearchResultDto
                {
                    Kind = "library",
                    Title = title,
                    Subtitle = $"{myMovie.Status} movie",
                    MatchedText = FirstSnippet(query,
                        ("Description", movie?.Description),
                        ("Genres", GenresText(movie))),
                    Route = $"/library/movies/{myMovie.MovieId}",
                    Icon = "film-outline",
                    MediaKind = "movies",
                    MediaId = myMovie.MovieId,
                    LibraryEntryId = myMovie.Id
                }));
        }
    }

    private static async Task AddSeriesHits(
        LuminaPathDbContext dbContext,
        string userId,
        string query,
        List<SearchHit> hits,
        CancellationToken cancellationToken)
    {
        var seriesItems = await dbContext.MySeries
            .AsNoTracking()
            .Include(mySeries => mySeries.Series)
            .Where(mySeries => mySeries.LuminaUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var mySeries in seriesItems)
        {
            var series = mySeries.Series;
            var title = series?.Name ?? "Untitled series";
            var score = Score(query, title, series?.Description, GenresText(series));
            if (score == 0)
            {
                continue;
            }

            hits.Add(new SearchHit(
                score,
                new GlobalSearchResultDto
                {
                    Kind = "library",
                    Title = title,
                    Subtitle = $"{mySeries.Status} series",
                    MatchedText = FirstSnippet(query,
                        ("Description", series?.Description),
                        ("Genres", GenresText(series))),
                    Route = $"/library/series/{mySeries.SeriesId}",
                    Icon = "tv-outline",
                    MediaKind = "series",
                    MediaId = mySeries.SeriesId,
                    LibraryEntryId = mySeries.Id
                }));
        }
    }

    private static async Task AddQuestHits(
        LuminaPathDbContext dbContext,
        string userId,
        string query,
        List<SearchHit> hits,
        CancellationToken cancellationToken)
    {
        var quests = await dbContext.Quests
            .AsNoTracking()
            .Include(quest => quest.Subtasks)
            .Include(quest => quest.MyGame)
                .ThenInclude(myGame => myGame!.Game)
            .Include(quest => quest.Skill)
            .Include(quest => quest.QuestFolder)
            .Where(quest => quest.LuminaUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var quest in quests)
        {
            var tags = string.Join(", ", quest.Tags);
            var subtasks = string.Join(", ", quest.Subtasks.Select(subtask => subtask.Title));
            var score = Score(
                query,
                quest.Title,
                quest.Notes,
                tags,
                quest.MyGame?.Game?.Name,
                quest.Skill?.Name,
                quest.QuestFolder?.Name,
                subtasks);
            if (score == 0)
            {
                continue;
            }

            var noteMatch = Matches(quest.Notes, query);
            hits.Add(new SearchHit(
                score + (noteMatch ? 10 : 0),
                new GlobalSearchResultDto
                {
                    Kind = "quest",
                    Title = quest.Title,
                    Subtitle = quest.MyGame?.Game?.Name ?? quest.Skill?.Name ?? quest.QuestFolder?.Name ?? "Quest",
                    MatchedText = FirstSnippet(query,
                        ("Notes", quest.Notes),
                        ("Tags", tags),
                        ("Subtasks", subtasks)),
                    Route = $"/quests?questId={quest.Id}",
                    Icon = noteMatch ? "document-text-outline" : "sparkles-outline",
                    QuestId = quest.Id,
                    LibraryEntryId = quest.MyGameId,
                    MediaKind = "quests"
                }));
        }
    }

    private static int Score(string query, params string?[] fields)
    {
        var score = 0;
        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            if (!Matches(field, query))
            {
                continue;
            }

            score = Math.Max(score, index switch
            {
                0 => 100,
                1 => 70,
                _ => 40
            });
        }

        return score;
    }

    private static bool Matches(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstSnippet(string query, params (string Label, string? Value)[] fields)
    {
        foreach (var (label, value) in fields)
        {
            if (!Matches(value, query))
            {
                continue;
            }

            return $"{label}: {Excerpt(value!, query)}";
        }

        return null;
    }

    private static string Excerpt(string value, string query)
    {
        var index = value.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return value.Length <= 140 ? value : $"{value[..137]}...";
        }

        var start = Math.Max(0, index - 42);
        var end = Math.Min(value.Length, index + query.Length + 72);
        var prefix = start > 0 ? "..." : string.Empty;
        var suffix = end < value.Length ? "..." : string.Empty;
        return $"{prefix}{value[start..end].Trim()}{suffix}";
    }

    private static string GenresText(Media? media)
    {
        return media is null ? string.Empty : string.Join(", ", media.Genres);
    }

    private sealed record SearchHit(int Score, GlobalSearchResultDto Result);
}
