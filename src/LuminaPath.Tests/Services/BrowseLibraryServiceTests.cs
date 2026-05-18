using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services;

public class BrowseLibraryServiceTests
{
    [Fact]
    public async Task GetGameReleasesAsync_ReturnsReleasedRootGamesWithCountsAndUserEntry()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.Games.AddRange(
                new Game
                {
                    Id = 1,
                    Name = "Older Popular",
                    ReleaseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Genres = ["RPG"],
                    Platforms = Platforms.PC,
                    Playtime = 40
                },
                new Game
                {
                    Id = 2,
                    Name = "Newest",
                    ReleaseDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Genres = ["Action"],
                    Platforms = Platforms.Playstation5
                },
                new Game
                {
                    Id = 3,
                    Name = "DLC",
                    ParentGameId = 1,
                    ReleaseDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Game
                {
                    Id = 4,
                    Name = "No Release Date"
                });
            context.MyGames.AddRange(
                new MyGame
                {
                    GameId = 1,
                    LuminaUserId = "viewer",
                    Status = GameStatus.Playing,
                    Rating = 8,
                    TimeSpend = 12,
                    MyGameInfo = new MyGameInfo
                    {
                        TrackedHours = 5.5,
                        FirstPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                        LastPlayed = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
                    }
                },
                new MyGame { GameId = 1, LuminaUserId = "other-a" },
                new MyGame { GameId = 1, LuminaUserId = "other-a" },
                new MyGame { GameId = 2, LuminaUserId = "other-b" });
            await context.SaveChangesAsync();
        }

        var service = new BrowseLibraryService(new TestDbContextFactory(options));

        var releases = await service.GetGameReleasesAsync("viewer", CancellationToken.None);

        Assert.Collection(
            releases,
            newest =>
            {
                Assert.Equal("Newest", newest.Name);
                Assert.Equal(1, newest.AddedCount);
                Assert.Null(newest.LibraryEntry);
            },
            older =>
            {
                Assert.Equal("Older Popular", older.Name);
                Assert.Equal(2, older.AddedCount);
                Assert.Equal("RPG", older.Genre);
                Assert.Equal((int)Platforms.PC, older.Platforms);
                Assert.Equal(40, older.Playtime);
                Assert.NotNull(older.LibraryEntry);
                Assert.Equal((int)GameStatus.Playing, older.LibraryEntry!.Status);
                Assert.Equal(5.5, older.LibraryEntry.MyGameInfo?.TrackedHours);
            });
    }

    [Fact]
    public async Task GetAnimeReleasesAsync_ReturnsViewerLibraryEntry()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.Animes.Add(new Anime
            {
                Id = 1,
                Name = "Season One",
                ReleaseDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                Genres = ["Drama"],
                EpisodeCount = 12,
                ExpectedWatchTimePerEpisodeMinutes = 24
            });
            context.MyAnimes.Add(new MyAnime
            {
                AnimeId = 1,
                LuminaUserId = "viewer",
                Status = MediaStatus.Watching,
                CurrentEpisode = 4,
                CurrentWatchTimeMinutes = 90,
                Rating = 9
            });
            await context.SaveChangesAsync();
        }

        var service = new BrowseLibraryService(new TestDbContextFactory(options));

        var releases = await service.GetAnimeReleasesAsync("viewer", CancellationToken.None);

        var release = Assert.Single(releases);
        Assert.Equal("Season One", release.Name);
        Assert.Equal("animes", release.Kind);
        Assert.Equal(12, release.EpisodeCount);
        Assert.Equal(24, release.ExpectedWatchTimePerEpisodeMinutes);
        Assert.Equal(1, release.AddedCount);
        Assert.Equal(4, release.LibraryEntry?.CurrentEpisode);
        Assert.Equal(90, release.LibraryEntry?.CurrentWatchTimeMinutes);
    }
}
