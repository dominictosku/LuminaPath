using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Features.Media.Models;

namespace LuminaPath.Tests.Features;

public class RedesignMediaTests
{
    [Fact]
    public void LibraryPage_DeclaresExpectedRoutesAndAuthorization()
    {
        var source = ReadServerSource("Features", "Media", "Library.razor");

        Assert.Contains("@attribute [Authorize]", source);
        Assert.Contains("@page \"/library\"", source);
        Assert.Contains("@page \"/Media/Library\"", source);
        Assert.Contains("@page \"/Media/MyGames\"", source);
        Assert.Contains("@page \"/Media/MyAnimes\"", source);
        Assert.Contains("@page \"/Media/MyMovies\"", source);
        Assert.Contains("@page \"/Media/MySeries\"", source);
    }

    [Fact]
    public void CatalogPage_DeclaresExpectedRoutesAndAuthorization()
    {
        var source = ReadServerSource("Features", "Media", "Catalog.razor");

        Assert.Contains("@attribute [Authorize]", source);
        Assert.Contains("@page \"/catalog\"", source);
        Assert.Contains("@page \"/Media/Catalog\"", source);
        Assert.Contains("@page \"/Media/Games\"", source);
        Assert.Contains("@page \"/Media/Animes\"", source);
        Assert.Contains("@page \"/Media/Movies\"", source);
        Assert.Contains("@page \"/Media/Series\"", source);
    }

    [Fact]
    public void LegacySeparateMediaPages_AreRemoved()
    {
        var root = FindRepositoryRoot();
        var legacyFiles = new[]
        {
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "Games", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "Games", "Index.razor.css"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "Animes", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "Movies", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "Series", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "MyGames", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "MyGames", "Index.razor.cs"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "MyGames", "Index.razor.css"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "MyAnimes", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "MyMovies", "Index.razor"),
            Path.Combine(root, "src", "LuminaPath", "Features", "Media", "MySeries", "Index.razor")
        };

        foreach (var file in legacyFiles)
        {
            Assert.False(File.Exists(file), $"Expected legacy file '{file}' to be deleted.");
        }
    }

    [Fact]
    public void AssetsDirectory_DoesNotContainHardcodedCopyrightImages()
    {
        var root = FindRepositoryRoot();
        var assetsDir = Path.Combine(root, "src", "LuminaPath", "wwwroot", "assets");
        if (Directory.Exists(assetsDir))
        {
            var files = Directory.GetFiles(assetsDir, "*.jpg");
            Assert.Empty(files);
        }
    }

    [Fact]
    public void UnifiedMediaItem_FromGame_PreservesStrictSeparationBetweenManualAndTrackedHours()
    {
        var game = new Game
        {
            Id = 1,
            Name = "Chrono Odyssey",
            Playtime = 80
        };

        var myGame = new MyGame
        {
            Id = 10,
            GameId = 1,
            Game = game,
            Status = GameStatus.Playing,
            TimeSpend = 14.5, // Manual logged hours
            MyGameInfo = new MyGameInfo
            {
                TrackedHours = 32.0 // Imported Steam/PSN hours
            }
        };

        var item = UnifiedMediaItem.FromGame(game, myGame);

        Assert.Equal(14.5, item.ManualHours);
        Assert.Equal(32.0, item.TrackedHours);
        Assert.Equal(46.5, item.TotalHours);
        Assert.Equal(80.0, item.EstimatedHours);
        Assert.Equal("14.5h", UnifiedMediaItem.FormatHours(item.ManualHours));
        Assert.Equal("32h", UnifiedMediaItem.FormatHours(item.TrackedHours));
        Assert.Equal("46.5h", UnifiedMediaItem.FormatHours(item.TotalHours));
    }

    [Fact]
    public void UnifiedMediaItem_ResolveCoverUrl_UsesUploadedDocumentOrFallbackPlaceholder()
    {
        // 1. With uploaded document image URL
        var gameWithDoc = new Game
        {
            Id = 1,
            Name = "Baldur's Gate 3",
            Image = new MediaDocument { StorageName = "guid_cover.jpg" }
        };
        var itemWithDoc = UnifiedMediaItem.FromGame(gameWithDoc);
        Assert.Equal("/api/files/guid_cover.jpg", itemWithDoc.CoverUrl);

        // 2. Without document image -> fallback placeholder, never hardcoded /assets/*.jpg
        var gameWithoutDoc = new Game
        {
            Id = 2,
            Name = "Expedition 33"
        };
        var itemWithoutDoc = UnifiedMediaItem.FromGame(gameWithoutDoc);
        Assert.Equal("/Images/Placeholder.png", itemWithoutDoc.CoverUrl);
        Assert.DoesNotContain("expedition", itemWithoutDoc.CoverUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("baldur", itemWithoutDoc.CoverUrl, StringComparison.OrdinalIgnoreCase);

        // 3. Absolute HTTP URL
        var externalUrl = UnifiedMediaItem.ResolveCoverUrl("https://example.com/artwork.webp");
        Assert.Equal("https://example.com/artwork.webp", externalUrl);
    }

    [Fact]
    public void UnifiedMediaItem_ProgressCalculations_AreAccurateForDifferentMediaTypes()
    {
        // Game with playtime estimate
        var game = new Game { Id = 1, Name = "RPG", Playtime = 100 };
        var myGame = new MyGame { Id = 1, GameId = 1, Game = game, Status = GameStatus.Playing, TimeSpend = 25 };
        var gameItem = UnifiedMediaItem.FromGame(game, myGame);
        Assert.Equal(25, gameItem.ProgressPercent);

        // Completed game is always 100%
        gameItem.GameStatus = GameStatus.Completed;
        Assert.Equal(100, gameItem.ProgressPercent);

        // Series with episodes
        var series = new Core.Models.Series { Id = 2, Name = "Show", EpisodeCount = 20 };
        var mySeries = new Core.Models.MySeries { Id = 2, SeriesId = 2, Series = series, CurrentEpisode = 10, Status = MediaStatus.Watching };
        var seriesItem = UnifiedMediaItem.FromSeries(series, mySeries);
        Assert.Equal(50, seriesItem.ProgressPercent);

        // Anime with episodes
        var anime = new Anime { Id = 3, Name = "Animation", EpisodeCount = 24 };
        var myAnime = new MyAnime { Id = 3, AnimeId = 3, Anime = anime, CurrentEpisode = 12, Status = MediaStatus.Watching };
        var animeItem = UnifiedMediaItem.FromAnime(anime, myAnime);
        Assert.Equal(50, animeItem.ProgressPercent);

        // Movie with watch time minutes
        var movie = new Movie { Id = 4, Name = "Film", ExpectedWatchTimeMinutes = 120 };
        var myMovie = new MyMovie { Id = 4, MovieId = 4, Movie = movie, CurrentWatchTimeMinutes = 90, Status = MediaStatus.Watching };
        var movieItem = UnifiedMediaItem.FromMovie(movie, myMovie);
        Assert.Equal(75, movieItem.ProgressPercent);

        // Item not in library
        var catalogItem = UnifiedMediaItem.FromGame(game);
        Assert.Equal(0, catalogItem.ProgressPercent);
        Assert.False(catalogItem.InLibrary);
    }

    [Fact]
    public void UnifiedMediaItem_NormalizedStatus_MapsAccurately()
    {
        var game = new Game { Id = 1, Name = "Game" };
        var myGame = new MyGame { Id = 1, GameId = 1, Status = GameStatus.MainGame };
        var item = UnifiedMediaItem.FromGame(game, myGame);
        Assert.Equal("playing", item.NormalizedStatus);
        Assert.True(item.IsActiveJourney);

        item.GameStatus = GameStatus.OnHold;
        Assert.Equal("backlog", item.NormalizedStatus);

        item.GameStatus = GameStatus.StoryComplete;
        Assert.Equal("completed", item.NormalizedStatus);
        Assert.True(item.IsCompleted);
    }

    [Fact]
    public void CatalogPage_DialogParameters_IncludeOnSubmitFileAndOnDeleteImage()
    {
        var source = ReadServerSource("Features", "Media", "UnifiedMediaPageBase.cs");

        Assert.Contains("{ x => x.OnSubmitFile", source);
        Assert.Contains("{ x => x.OnDeleteImage", source);
        Assert.Contains("HandleSubmitFileAsync", source);
        Assert.Contains("HandleDeleteImageAsync", source);
    }

    private static string ReadServerSource(params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "LuminaPath", Path.Combine(pathParts)));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "LuminaPath", "LuminaPath.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the LuminaPath repository root.");
    }
}
