using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;

namespace LuminaPath.Features.Media.Models;

public sealed class UnifiedMediaItem
{
    public string Kind { get; init; } = "game"; // "game", "movie", "series", "anime"
    public int MediaId { get; init; }
    public int? UserMediaId { get; set; }
    public bool InLibrary => UserMediaId.HasValue;

    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public int? Year => ReleaseDate?.Year;
    public List<string> Genres { get; init; } = [];
    public string PrimaryGenre => Genres.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g)) ?? KindLabel;
    public string Source { get; init; } = "Lumina";
    public string Platform { get; init; } = "PC";
    public string CoverUrl { get; init; } = "/Images/Placeholder.png";

    // Status & Ratings
    public GameStatus? GameStatus { get; set; }
    public MediaStatus? MediaStatus { get; set; }
    public short? Rating { get; set; }
    public int Priority { get; set; } = 1;
    public string? PersonalNotes { get; set; }

    // Playtime / Watch time separation (Manual vs Tracked)
    public double ManualHours { get; set; }
    public double TrackedHours { get; init; }
    public double TotalHours => Math.Round(ManualHours + TrackedHours, 1);
    public double EstimatedHours { get; init; }

    // Episode / Movie minute tracking
    public int? CurrentEpisode { get; set; }
    public int? TotalEpisodes { get; init; }
    public int? EpisodeDurationMinutes { get; init; }
    public int? CurrentWatchTimeMinutes { get; set; }
    public int? ExpectedWatchTimeMinutes { get; init; }

    // Raw domain references for full dialog editing
    public Game? GameEntity { get; init; }
    public MyGame? MyGameEntity { get; set; }
    public Movie? MovieEntity { get; init; }
    public MyMovie? MyMovieEntity { get; set; }
    public Core.Models.Series? SeriesEntity { get; init; }
    public Core.Models.MySeries? MySeriesEntity { get; set; }
    public Anime? AnimeEntity { get; init; }
    public MyAnime? MyAnimeEntity { get; set; }

    public string KindLabel => Kind switch
    {
        "game" => "Game",
        "movie" => "Movie",
        "series" => "Series",
        "anime" => "Anime",
        _ => "Media"
    };

    public string KindIcon => Kind switch
    {
        "game" => "⌘",
        "movie" => "▻",
        "series" => "▤",
        "anime" => "▤",
        _ => "✧"
    };

    /// <summary>
    /// Normalized status key matching the redesign filters:
    /// "playing", "watching", "completed", "backlog", "wishlist", "dropped", or "catalog".
    /// </summary>
    public string NormalizedStatus
    {
        get
        {
            if (!InLibrary)
            {
                return "catalog";
            }

            if (Kind == "game" && GameStatus.HasValue)
            {
                return GameStatus.Value switch
                {
                    Core.Enums.GameStatus.Playing or Core.Enums.GameStatus.MainGame => "playing",
                    Core.Enums.GameStatus.Completed or Core.Enums.GameStatus.StoryComplete => "completed",
                    Core.Enums.GameStatus.OnHold => "backlog",
                    _ => "backlog"
                };
            }

            if (MediaStatus.HasValue)
            {
                return MediaStatus.Value switch
                {
                    Core.Enums.MediaStatus.Watching => "watching",
                    Core.Enums.MediaStatus.Completed => "completed",
                    Core.Enums.MediaStatus.Dropped => "dropped",
                    _ => "backlog"
                };
            }

            return "backlog";
        }
    }

    public bool IsActiveJourney => InLibrary && (NormalizedStatus == "playing" || NormalizedStatus == "watching");

    public bool IsCompleted => InLibrary && NormalizedStatus == "completed";

    public string StatusDisplayLabel
    {
        get
        {
            if (!InLibrary)
            {
                return "Catalog";
            }

            if (Kind == "game" && GameStatus.HasValue)
            {
                return GameStatus.Value switch
                {
                    Core.Enums.GameStatus.MainGame => "Main game",
                    Core.Enums.GameStatus.Playing => "Playing",
                    Core.Enums.GameStatus.StoryComplete => "Story complete",
                    Core.Enums.GameStatus.Completed => "Completed",
                    Core.Enums.GameStatus.OnHold => "On hold",
                    _ => "Planned"
                };
            }

            if (MediaStatus.HasValue)
            {
                return MediaStatus.Value switch
                {
                    Core.Enums.MediaStatus.Watching => "Watching",
                    Core.Enums.MediaStatus.Completed => "Completed",
                    Core.Enums.MediaStatus.OnHold => "On hold",
                    Core.Enums.MediaStatus.Dropped => "Dropped",
                    _ => "Planned"
                };
            }

            return "Backlog";
        }
    }

    public string StatusDotClass => NormalizedStatus switch
    {
        "playing" => "s-playing",
        "watching" => "s-watching",
        "completed" => "s-completed",
        "wishlist" => "s-wishlist",
        "dropped" => "s-dropped",
        _ => "s-backlog"
    };

    public int ProgressPercent
    {
        get
        {
            if (IsCompleted)
            {
                return 100;
            }

            if (!InLibrary)
            {
                return 0;
            }

            if ((Kind == "series" || Kind == "anime") && TotalEpisodes is > 0 && CurrentEpisode is > 0)
            {
                return Math.Clamp((int)Math.Round(CurrentEpisode.Value * 100.0 / TotalEpisodes.Value), 4, 100);
            }

            if (Kind == "movie" && ExpectedWatchTimeMinutes is > 0)
            {
                var watchedMinutes = CurrentWatchTimeMinutes ?? (int)Math.Round(ManualHours * 60);
                if (watchedMinutes > 0)
                {
                    return Math.Clamp((int)Math.Round(watchedMinutes * 100.0 / ExpectedWatchTimeMinutes.Value), 4, 100);
                }
            }

            if (EstimatedHours > 0 && TotalHours > 0)
            {
                return Math.Clamp((int)Math.Round(TotalHours * 100.0 / EstimatedHours), 4, 100);
            }

            return IsActiveJourney ? 35 : 0;
        }
    }

    public string CardFootText
    {
        get
        {
            if ((Kind == "series" || Kind == "anime") && InLibrary && CurrentEpisode is > 0)
            {
                return TotalEpisodes is > 0
                    ? $"Ep {CurrentEpisode}/{TotalEpisodes}"
                    : $"Ep {CurrentEpisode}";
            }

            if (TotalHours > 0)
            {
                return EstimatedHours > 0
                    ? $"{FormatHours(TotalHours)} / {FormatHours(EstimatedHours)}"
                    : $"{FormatHours(TotalHours)} logged";
            }

            if (Rating is > 0)
            {
                return $"★ {Rating}/10";
            }

            if ((Kind == "series" || Kind == "anime") && TotalEpisodes is > 0)
            {
                return $"{TotalEpisodes} eps";
            }

            if (EstimatedHours > 0)
            {
                return $"~{FormatHours(EstimatedHours)}";
            }

            return Year?.ToString() ?? Source;
        }
    }

    public static string FormatHours(double hours)
    {
        return hours % 1 == 0 ? $"{(int)hours}h" : $"{hours:0.#}h";
    }

    public static UnifiedMediaItem FromGame(Game game, MyGame? myGame = null)
    {
        myGame ??= game.MyGames?.FirstOrDefault();
        var manual = Math.Max(0, myGame?.TimeSpend ?? 0);
        var tracked = Math.Max(0, myGame?.MyGameInfo?.TrackedHours ?? 0);
        var platform = game.Platforms != default
            ? game.Platforms.ToString()
            : (!string.IsNullOrWhiteSpace(game.Source) ? game.Source : "PC");

        return new UnifiedMediaItem
        {
            Kind = "game",
            MediaId = game.Id,
            UserMediaId = myGame?.Id,
            Title = game.Name,
            Description = game.Description,
            ReleaseDate = game.ReleaseDate,
            Genres = game.Genres ?? [],
            Source = string.IsNullOrWhiteSpace(game.Source) ? "Lumina" : game.Source,
            Platform = platform,
            CoverUrl = ResolveCoverUrl(game.Image?.Url ?? game.Image?.Path ?? myGame?.Game?.Image?.Url ?? myGame?.Game?.Image?.Path),
            GameStatus = myGame?.Status,
            Rating = myGame?.Rating,
            Priority = myGame?.Priority is > 0 ? myGame.Priority : 1,
            PersonalNotes = myGame?.PersonalNotes,
            ManualHours = manual,
            TrackedHours = tracked,
            EstimatedHours = Math.Max(0, game.Playtime ?? 0),
            GameEntity = game,
            MyGameEntity = myGame
        };
    }

    public static UnifiedMediaItem FromMyGame(MyGame myGame)
    {
        var game = myGame.Game ?? new Game { Id = myGame.GameId, Name = $"Game #{myGame.GameId}" };
        return FromGame(game, myGame);
    }

    public static UnifiedMediaItem FromMovie(Movie movie, MyMovie? myMovie = null)
    {
        myMovie ??= movie.MyMovies?.FirstOrDefault();
        var manualHours = myMovie?.TimeSpend ?? ((myMovie?.CurrentWatchTimeMinutes ?? 0) / 60.0);
        var estHours = (movie.ExpectedWatchTimeMinutes ?? 0) / 60.0;

        return new UnifiedMediaItem
        {
            Kind = "movie",
            MediaId = movie.Id,
            UserMediaId = myMovie?.Id,
            Title = movie.Name,
            Description = movie.Description,
            ReleaseDate = movie.ReleaseDate,
            Genres = movie.Genres ?? [],
            Source = string.IsNullOrWhiteSpace(movie.Source) ? "Cinema" : movie.Source,
            Platform = string.IsNullOrWhiteSpace(movie.Source) ? "Cinema" : movie.Source,
            CoverUrl = ResolveCoverUrl(movie.Image?.Url ?? movie.Image?.Path ?? myMovie?.Movie?.Image?.Url ?? myMovie?.Movie?.Image?.Path),
            MediaStatus = myMovie?.Status,
            Rating = myMovie?.Rating,
            Priority = myMovie?.Priority is > 0 ? myMovie.Priority : 1,
            ManualHours = Math.Round(Math.Max(0, manualHours), 1),
            TrackedHours = 0,
            EstimatedHours = Math.Round(Math.Max(0, estHours), 1),
            CurrentWatchTimeMinutes = myMovie?.CurrentWatchTimeMinutes,
            ExpectedWatchTimeMinutes = movie.ExpectedWatchTimeMinutes,
            MovieEntity = movie,
            MyMovieEntity = myMovie
        };
    }

    public static UnifiedMediaItem FromMyMovie(MyMovie myMovie)
    {
        var movie = myMovie.Movie ?? new Movie { Id = myMovie.MovieId, Name = $"Movie #{myMovie.MovieId}" };
        return FromMovie(movie, myMovie);
    }

    public static UnifiedMediaItem FromSeries(Core.Models.Series series, Core.Models.MySeries? mySeries = null)
    {
        mySeries ??= series.MySeries?.FirstOrDefault();
        var manualHours = mySeries?.TimeSpend
            ?? (((mySeries?.CurrentEpisode ?? 0) * (series.ExpectedWatchTimePerEpisodeMinutes ?? 0)) / 60.0);
        var estHours = (series.ExpectedWatchTimeMinutes ?? 0) / 60.0;

        return new UnifiedMediaItem
        {
            Kind = "series",
            MediaId = series.Id,
            UserMediaId = mySeries?.Id,
            Title = series.Name,
            Description = series.Description,
            ReleaseDate = series.ReleaseDate,
            Genres = series.Genres ?? [],
            Source = string.IsNullOrWhiteSpace(series.Source) ? "Streaming" : series.Source,
            Platform = string.IsNullOrWhiteSpace(series.Source) ? "Streaming" : series.Source,
            CoverUrl = ResolveCoverUrl(series.Image?.Url ?? series.Image?.Path ?? mySeries?.Series?.Image?.Url ?? mySeries?.Series?.Image?.Path),
            MediaStatus = mySeries?.Status,
            Rating = mySeries?.Rating,
            Priority = mySeries?.Priority is > 0 ? mySeries.Priority : 1,
            ManualHours = Math.Round(Math.Max(0, manualHours), 1),
            TrackedHours = 0,
            EstimatedHours = Math.Round(Math.Max(0, estHours), 1),
            CurrentEpisode = mySeries?.CurrentEpisode,
            TotalEpisodes = series.EpisodeCount,
            EpisodeDurationMinutes = series.ExpectedWatchTimePerEpisodeMinutes,
            CurrentWatchTimeMinutes = mySeries?.CurrentWatchTimeMinutes,
            ExpectedWatchTimeMinutes = series.ExpectedWatchTimeMinutes,
            SeriesEntity = series,
            MySeriesEntity = mySeries
        };
    }

    public static UnifiedMediaItem FromMySeries(Core.Models.MySeries mySeries)
    {
        var series = mySeries.Series ?? new Core.Models.Series { Id = mySeries.SeriesId, Name = $"Series #{mySeries.SeriesId}" };
        return FromSeries(series, mySeries);
    }

    public static UnifiedMediaItem FromAnime(Anime anime, MyAnime? myAnime = null)
    {
        myAnime ??= anime.MyAnimes?.FirstOrDefault();
        var manualHours = myAnime?.TimeSpend
            ?? (((myAnime?.CurrentEpisode ?? 0) * (anime.ExpectedWatchTimePerEpisodeMinutes ?? 0)) / 60.0);
        var estHours = (anime.ExpectedWatchTimeMinutes ?? 0) / 60.0;

        return new UnifiedMediaItem
        {
            Kind = "anime",
            MediaId = anime.Id,
            UserMediaId = myAnime?.Id,
            Title = anime.Name,
            Description = anime.Description,
            ReleaseDate = anime.ReleaseDate,
            Genres = anime.Genres ?? [],
            Source = string.IsNullOrWhiteSpace(anime.Source) ? "Crunchyroll" : anime.Source,
            Platform = string.IsNullOrWhiteSpace(anime.Source) ? "Crunchyroll" : anime.Source,
            CoverUrl = ResolveCoverUrl(anime.Image?.Url ?? anime.Image?.Path ?? myAnime?.Anime?.Image?.Url ?? myAnime?.Anime?.Image?.Path),
            MediaStatus = myAnime?.Status,
            Rating = myAnime?.Rating,
            Priority = myAnime?.Priority is > 0 ? myAnime.Priority : 1,
            ManualHours = Math.Round(Math.Max(0, manualHours), 1),
            TrackedHours = 0,
            EstimatedHours = Math.Round(Math.Max(0, estHours), 1),
            CurrentEpisode = myAnime?.CurrentEpisode,
            TotalEpisodes = anime.EpisodeCount,
            EpisodeDurationMinutes = anime.ExpectedWatchTimePerEpisodeMinutes,
            CurrentWatchTimeMinutes = myAnime?.CurrentWatchTimeMinutes,
            ExpectedWatchTimeMinutes = anime.ExpectedWatchTimeMinutes,
            AnimeEntity = anime,
            MyAnimeEntity = myAnime
        };
    }

    public static UnifiedMediaItem FromMyAnime(MyAnime myAnime)
    {
        var anime = myAnime.Anime ?? new Anime { Id = myAnime.AnimeId, Name = $"Anime #{myAnime.AnimeId}" };
        return FromAnime(anime, myAnime);
    }

    public static string ResolveCoverUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return "/Images/Placeholder.png";
        }

        var trimmed = rawUrl.Trim();
        return trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("/", StringComparison.Ordinal)
            ? trimmed
            : $"/{trimmed}";
    }
}
