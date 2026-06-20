using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;

namespace Test.Services
{
    public class ExcelServiceTests
    {
        [Fact]
        public async Task ImportLibraryWorkbookAsync_CreatesGameAndMyGame_FromWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateWorkbookStream();
            var result = await service.ImportLibraryWorkbookAsync(stream, user);

            await using var assertContext = new LuminaPathDbContext(options);
            var game = await assertContext.Games
                .Include(g => g.ExternalIds)
                .Include(g => g.MyGames!)
                    .ThenInclude(g => g.MyGameInfo)
                .SingleAsync();
            var myGame = game.MyGames!.Single();
            Assert.Empty(result.Errors);
            Assert.Equal(1, result.RowsImported);
            Assert.Equal(1, result.CreatedMedia);
            Assert.Equal(1, result.CreatedLibraryItems);
            Assert.Equal("Test Game", game.Name);
            Assert.Equal("PSN-123", game.ExternalIds.GetExternalId(ExternalMediaProvider.Psn));
            Assert.Equal(GameStatus.Playing, myGame.Status);
            Assert.Equal(user.Id, myGame.LuminaUserId);
            Assert.NotNull(myGame.MyGameInfo);
            Assert.Equal(DateTimeKind.Utc, game.ReleaseDate!.Value.Kind);
            Assert.Equal(DateTimeKind.Utc, myGame.StartDate!.Value.Kind);
            Assert.Equal(DateTimeKind.Utc, myGame.MyGameInfo!.FirstPlayed.Kind);
            Assert.Equal(DateTimeKind.Utc, myGame.MyGameInfo.LastPlayed.Kind);
        }

        [Fact]
        public async Task PreviewLibraryWorkbookAsync_MarksDuplicateRows_FromWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateWorkbookStream(includeDuplicate: true);

            var preview = await service.PreviewLibraryWorkbookAsync(stream, user);

            Assert.Equal(2, preview.RowsDetected);
            Assert.Equal(1, preview.DuplicateRows);
            Assert.Contains(preview.Rows, row => row.ChangeType == "New");
            Assert.Contains(preview.Rows, row => row.ChangeType == "Duplicate");
        }

        [Fact]
        public async Task ImportLibraryWorkbookAsync_SkipsDuplicateRows_FromWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateWorkbookStream(includeDuplicate: true);

            var result = await service.ImportLibraryWorkbookAsync(stream, user);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, result.RowsImported);
            Assert.Single(await assertContext.Games.ToListAsync());
            Assert.Contains(result.Errors, error => error.Contains("duplicate import row skipped", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ExportLibraryWorkbookAsync_WritesCurrentLibraryWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);

            await using (var context = new LuminaPathDbContext(options))
            {
                context.MyGames.Add(new MyGame
                {
                    LuminaUserId = user.Id,
                    Status = GameStatus.Playing,
                    Priority = 2,
                    Rating = 9,
                    PersonalNotes = "Try heat 16",
                    StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    TimeSpend = 14.5,
                    MyGameInfo = new MyGameInfo
                    {
                        TrackedHours = 12.25,
                        FirstPlayed = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                        LastPlayed = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc)
                    },
                    Game = new Game
                    {
                        Name = "Hades",
                        Source = "Steam",
                        Genres = ["Action", "Roguelike"],
                        Platforms = Platforms.PC | Platforms.Switch,
                        Playtime = 30,
                        ParentGameId = 99,
                        Image = new MediaDocument { StorageName = "hades.png" },
                        ExternalIds =
                        [
                            new MediaExternalId { Provider = ExternalMediaProvider.Psn, ExternalId = "PSN-HADES" },
                            new MediaExternalId { Provider = ExternalMediaProvider.Steam, ExternalId = "1145360" },
                            new MediaExternalId { Provider = ExternalMediaProvider.Igdb, ExternalId = "IGDB-HADES" },
                            new MediaExternalId { Provider = ExternalMediaProvider.Rawg, ExternalId = "RAWG-HADES" }
                        ]
                    }
                });
                context.MyAnimes.Add(new MyAnime
                {
                    LuminaUserId = user.Id,
                    Status = MediaStatus.Watching,
                    Priority = 3,
                    Rating = 10,
                    CurrentEpisode = 4,
                    CurrentWatchTimeMinutes = 100,
                    Anime = new Anime
                    {
                        Name = "Frieren",
                        Source = "AniList",
                        Genres = ["Fantasy"],
                        EpisodeCount = 28,
                        ExpectedWatchTimePerEpisodeMinutes = 25,
                        ParentAnimeId = 7,
                        Image = new MediaDocument { StorageName = "frieren.jpg" },
                        ExternalIds =
                        [
                            new MediaExternalId { Provider = ExternalMediaProvider.Anilist, ExternalId = "154587" },
                            new MediaExternalId { Provider = ExternalMediaProvider.Mal, ExternalId = "52991" }
                        ]
                    }
                });
                context.MyMovies.Add(new MyMovie
                {
                    LuminaUserId = user.Id,
                    Status = MediaStatus.Planned,
                    CurrentWatchTimeMinutes = 45,
                    Movie = new Movie
                    {
                        Name = "Dune",
                        Source = "TMDB",
                        ExpectedWatchTimeMinutes = 155,
                        ExternalIds = [new MediaExternalId { Provider = ExternalMediaProvider.Tmdb, ExternalId = "438631" }]
                    }
                });
                context.MySeries.Add(new MySeries
                {
                    LuminaUserId = user.Id,
                    Status = MediaStatus.Watching,
                    CurrentEpisode = 5,
                    CurrentWatchTimeMinutes = 250,
                    Series = new Series
                    {
                        Name = "Severance",
                        Source = "TMDB",
                        EpisodeCount = 9,
                        ExpectedWatchTimePerEpisodeMinutes = 50,
                        ParentSeriesId = 12,
                        ExternalIds = [new MediaExternalId { Provider = ExternalMediaProvider.Tmdb, ExternalId = "95396" }]
                    }
                });
                context.MyGames.Add(new MyGame
                {
                    LuminaUserId = "other-user",
                    Game = new Game { Name = "Other user game", Source = "Manual" }
                });
                await context.SaveChangesAsync();
            }

            var bytes = await service.ExportLibraryWorkbookAsync(user);

            using var workbook = new XLWorkbook(new MemoryStream(bytes));
            Assert.Contains("Games", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Animes", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Movies", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Series", workbook.Worksheets.Select(sheet => sheet.Name));

            var games = workbook.Worksheet("Games");
            var gameHeaders = HeaderMap(games);
            Assert.Equal("Hades", games.Cell(2, gameHeaders["Name"]).GetString());
            Assert.Equal("Try heat 16", games.Cell(2, gameHeaders["Personal Notes"]).GetString());
            Assert.Equal("1145360", games.Cell(2, gameHeaders["SteamId"]).GetString());
            Assert.Equal("IGDB-HADES", games.Cell(2, gameHeaders["IGDBId"]).GetString());
            Assert.Equal("RAWG-HADES", games.Cell(2, gameHeaders["RAWGId"]).GetString());
            Assert.Equal("api/files/hades.png", games.Cell(2, gameHeaders["Image Url"]).GetString());
            Assert.Equal(99, games.Cell(2, gameHeaders["Parent Game Id"]).GetValue<int>());
            Assert.True(games.Cell(3, gameHeaders["Name"]).IsEmpty());

            var animes = workbook.Worksheet("Animes");
            var animeHeaders = HeaderMap(animes);
            Assert.Equal("Frieren", animes.Cell(2, animeHeaders["Name"]).GetString());
            Assert.Equal("154587", animes.Cell(2, animeHeaders["AniListId"]).GetString());
            Assert.Equal("52991", animes.Cell(2, animeHeaders["MALId"]).GetString());
            Assert.Equal(28, animes.Cell(2, animeHeaders["Episode Count"]).GetValue<int>());
            Assert.Equal(700, animes.Cell(2, animeHeaders["Expected Watch Time Minutes"]).GetValue<int>());
            Assert.Equal(4, animes.Cell(2, animeHeaders["Current Episode"]).GetValue<int>());
            Assert.Equal(100, animes.Cell(2, animeHeaders["Current Watch Time Minutes"]).GetValue<int>());

            var movies = workbook.Worksheet("Movies");
            var movieHeaders = HeaderMap(movies);
            Assert.Equal("Dune", movies.Cell(2, movieHeaders["Name"]).GetString());
            Assert.Equal("438631", movies.Cell(2, movieHeaders["TMDBId"]).GetString());
            Assert.Equal(155, movies.Cell(2, movieHeaders["Expected Watch Time Minutes"]).GetValue<int>());
            Assert.Equal(45, movies.Cell(2, movieHeaders["Current Watch Time Minutes"]).GetValue<int>());

            var series = workbook.Worksheet("Series");
            var seriesHeaders = HeaderMap(series);
            Assert.Equal("Severance", series.Cell(2, seriesHeaders["Name"]).GetString());
            Assert.Equal("95396", series.Cell(2, seriesHeaders["TMDBId"]).GetString());
            Assert.Equal(9, series.Cell(2, seriesHeaders["Episode Count"]).GetValue<int>());
            Assert.Equal(450, series.Cell(2, seriesHeaders["Expected Watch Time Minutes"]).GetValue<int>());
            Assert.Equal(5, series.Cell(2, seriesHeaders["Current Episode"]).GetValue<int>());
            Assert.Equal(250, series.Cell(2, seriesHeaders["Current Watch Time Minutes"]).GetValue<int>());
        }

        [Fact]
        public async Task PreviewLibraryWorkbookAsync_ReadsCurrentLibrarySheets()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateLibraryWorkbookStream();

            var preview = await service.PreviewLibraryWorkbookAsync(stream, user);

            Assert.Empty(preview.Errors);
            Assert.Equal(4, preview.RowsDetected);
            Assert.Equal(4, preview.CreatedMedia);
            Assert.Equal(4, preview.CreatedLibraryItems);
            Assert.Contains(preview.Rows, row => row.MediaType == "Game" && row.ExternalId == "Psn:PSN-ROUNDTRIP");
            Assert.Contains(preview.Rows, row => row.MediaType == "Anime" && row.ExternalId == "Anilist:154587");
            Assert.Contains(preview.Rows, row => row.MediaType == "Movie" && row.ExternalId == "Tmdb:438631");
            Assert.Contains(preview.Rows, row => row.MediaType == "Series" && row.ExternalId == "Tmdb:95396");
        }

        [Fact]
        public async Task ImportLibraryWorkbookAsync_CreatesCurrentLibraryMedia()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateLibraryWorkbookStream();

            var result = await service.ImportLibraryWorkbookAsync(stream, user);

            Assert.Empty(result.Errors);
            Assert.Equal(4, result.RowsImported);
            Assert.Equal(4, result.CreatedMedia);
            Assert.Equal(4, result.CreatedLibraryItems);

            await using var assertContext = new LuminaPathDbContext(options);
            var game = await assertContext.Games
                .Include(item => item.ExternalIds)
                .Include(item => item.MyGames!)
                    .ThenInclude(item => item.MyGameInfo)
                .SingleAsync(item => item.Name == "Roundtrip Game");
            var myGame = game.MyGames!.Single();
            Assert.Equal("PSN-ROUNDTRIP", game.ExternalIds.GetExternalId(ExternalMediaProvider.Psn));
            Assert.Equal("12345", game.ExternalIds.GetExternalId(ExternalMediaProvider.Steam));
            Assert.Equal("IGDB-ROUNDTRIP", game.ExternalIds.GetExternalId(ExternalMediaProvider.Igdb));
            Assert.Equal("RAWG-ROUNDTRIP", game.ExternalIds.GetExternalId(ExternalMediaProvider.Rawg));
            Assert.Equal(GameStatus.Playing, myGame.Status);
            Assert.Equal((short)8, myGame.Rating);
            Assert.Equal(11.5, myGame.MyGameInfo!.TrackedHours);

            var anime = await assertContext.Animes
                .Include(item => item.ExternalIds)
                .Include(item => item.MyAnimes)
                .SingleAsync(item => item.Name == "Frieren");
            var myAnime = anime.MyAnimes!.Single();
            Assert.Equal("154587", anime.ExternalIds.GetExternalId(ExternalMediaProvider.Anilist));
            Assert.Equal("52991", anime.ExternalIds.GetExternalId(ExternalMediaProvider.Mal));
            Assert.Equal(28, anime.EpisodeCount);
            Assert.Equal(25, anime.ExpectedWatchTimePerEpisodeMinutes);
            Assert.Equal(MediaStatus.Watching, myAnime.Status);
            Assert.Equal(4, myAnime.CurrentEpisode);
            Assert.Equal(100, myAnime.CurrentWatchTimeMinutes);

            var movie = await assertContext.Movies
                .Include(item => item.ExternalIds)
                .Include(item => item.MyMovies)
                .SingleAsync(item => item.Name == "Dune");
            var myMovie = movie.MyMovies!.Single();
            Assert.Equal("438631", movie.ExternalIds.GetExternalId(ExternalMediaProvider.Tmdb));
            Assert.Equal(155, movie.ExpectedWatchTimeMinutes);
            Assert.Equal(MediaStatus.Planned, myMovie.Status);
            Assert.Equal(45, myMovie.CurrentWatchTimeMinutes);

            var series = await assertContext.Series
                .Include(item => item.ExternalIds)
                .Include(item => item.MySeries)
                .SingleAsync(item => item.Name == "Severance");
            var mySeries = series.MySeries!.Single();
            Assert.Equal("95396", series.ExternalIds.GetExternalId(ExternalMediaProvider.Tmdb));
            Assert.Equal(9, series.EpisodeCount);
            Assert.Equal(50, series.ExpectedWatchTimePerEpisodeMinutes);
            Assert.Equal(MediaStatus.Watching, mySeries.Status);
            Assert.Equal(5, mySeries.CurrentEpisode);
            Assert.Equal(250, mySeries.CurrentWatchTimeMinutes);
        }

        [Fact]
        public async Task ImportLibraryWorkbookAsync_RestoresGameParentAndPersonalNotes()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateGameParentWorkbookStream();

            var result = await service.ImportLibraryWorkbookAsync(stream, user);

            Assert.Empty(result.Errors);
            Assert.Equal(2, result.RowsImported);

            await using var assertContext = new LuminaPathDbContext(options);
            var child = await assertContext.Games
                .Include(game => game.ParentGame)
                .Include(game => game.MyGames!)
                .SingleAsync(game => game.Name == "Imported Expansion");

            Assert.Equal("Imported Base Game", child.ParentGame?.Name);
            Assert.Equal("Bring snacks", child.MyGames!.Single().PersonalNotes);
        }

        private static DbContextOptions<LuminaPathDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<LuminaPathDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static ExcelService CreateService(DbContextOptions<LuminaPathDbContext> options)
        {
            var dbContextFactory = new TestDbContextFactory(options);
            return new ExcelService(dbContextFactory, new GameImportPipeline(dbContextFactory));
        }

        private static async Task<LuminaUser> SeedUser(DbContextOptions<LuminaPathDbContext> options)
        {
            var user = new LuminaUser
            {
                Id = "user-1",
                UserName = "test@example.com",
                FullName = "Test User"
            };

            await using var context = new LuminaPathDbContext(options);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        private static MemoryStream CreateWorkbookStream(bool includeDuplicate = false)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Games");

            var headers = new[]
            {
                "Name",
                "Status",
                "Release Date",
                "PSNId",
                "Start Date",
                "First Played",
                "Last Played",
                "Tracked Hours"
            };

            for (var index = 0; index < headers.Length; index++)
            {
                worksheet.Cell(1, index + 1).Value = headers[index];
            }

            worksheet.Cell(2, 1).Value = "Test Game";
            worksheet.Cell(2, 2).Value = "In-Progress";
            worksheet.Cell(2, 3).Value = new DateTime(2024, 1, 5);
            worksheet.Cell(2, 4).Value = "PSN-123";
            worksheet.Cell(2, 5).Value = new DateTime(2024, 1, 6);
            worksheet.Cell(2, 6).Value = new DateTime(2024, 1, 7);
            worksheet.Cell(2, 7).Value = new DateTime(2024, 1, 8);
            worksheet.Cell(2, 8).Value = 4.5;

            if (includeDuplicate)
            {
                worksheet.Cell(3, 1).Value = "Test Game Duplicate";
                worksheet.Cell(3, 2).Value = "Planned";
                worksheet.Cell(3, 4).Value = "PSN-123";
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        private static MemoryStream CreateLibraryWorkbookStream()
        {
            using var workbook = new XLWorkbook();
            AddWorksheet(workbook, "Games",
            [
                "Name",
                "Status",
                "Priority",
                "Release Date",
                "Platform",
                "Genre",
                "Source",
                "Description",
                "Playtime",
                "PSNId",
                "SteamId",
                "IGDBId",
                "RAWGId",
                "Rating",
                "Start Date",
                "End Date",
                "Time Spend",
                "First Played",
                "Last Played",
                "Tracked Hours"
            ],
            [
                "Roundtrip Game",
                "In-Progress",
                2,
                new DateTime(2024, 1, 5),
                "PC, Switch",
                "Action, Roguelike",
                "Excel",
                "Roundtrip description",
                30,
                "PSN-ROUNDTRIP",
                "12345",
                "IGDB-ROUNDTRIP",
                "RAWG-ROUNDTRIP",
                8,
                new DateTime(2024, 1, 6),
                new DateTime(2024, 1, 7),
                12.75,
                new DateTime(2024, 1, 8),
                new DateTime(2024, 1, 9),
                11.5
            ]);

            AddWorksheet(workbook, "Animes",
            [
                "Name",
                "Status",
                "Priority",
                "Release Date",
                "Genre",
                "Source",
                "Description",
                "AniListId",
                "MALId",
                "Episode Count",
                "Expected Minutes Per Episode",
                "Expected Watch Time Minutes",
                "Rating",
                "Start Date",
                "End Date",
                "Time Spend",
                "Current Episode",
                "Current Watch Time Minutes"
            ],
            [
                "Frieren",
                "Watching",
                3,
                new DateTime(2023, 9, 29),
                "Fantasy",
                "AniList",
                "Beyond journey's end",
                "154587",
                "52991",
                28,
                25,
                700,
                10,
                new DateTime(2024, 2, 1),
                null,
                1.7,
                4,
                100
            ]);

            AddWorksheet(workbook, "Movies",
            [
                "Name",
                "Status",
                "Priority",
                "Release Date",
                "Genre",
                "Source",
                "Description",
                "TMDBId",
                "Expected Watch Time Minutes",
                "Rating",
                "Start Date",
                "End Date",
                "Time Spend",
                "Current Watch Time Minutes"
            ],
            [
                "Dune",
                "Planned",
                1,
                new DateTime(2021, 9, 3),
                "Sci-Fi",
                "TMDB",
                "Arrakis awaits",
                "438631",
                155,
                null,
                null,
                null,
                0.75,
                45
            ]);

            AddWorksheet(workbook, "Series",
            [
                "Name",
                "Status",
                "Priority",
                "Release Date",
                "Genre",
                "Source",
                "Description",
                "TMDBId",
                "Episode Count",
                "Expected Minutes Per Episode",
                "Expected Watch Time Minutes",
                "Rating",
                "Start Date",
                "End Date",
                "Time Spend",
                "Current Episode",
                "Current Watch Time Minutes"
            ],
            [
                "Severance",
                "Watching",
                4,
                new DateTime(2022, 2, 18),
                "Drama, Mystery",
                "TMDB",
                "Work-life balance",
                "95396",
                9,
                50,
                450,
                9,
                new DateTime(2024, 3, 1),
                null,
                4.2,
                5,
                250
            ]);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        private static MemoryStream CreateGameParentWorkbookStream()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Games");
            var headers = new[]
            {
                "Id",
                "Name",
                "Status",
                "Parent Game Id",
                "Personal Notes"
            };

            for (var index = 0; index < headers.Length; index++)
            {
                worksheet.Cell(1, index + 1).Value = headers[index];
            }

            worksheet.Cell(2, 1).Value = 10;
            worksheet.Cell(2, 2).Value = "Imported Base Game";
            worksheet.Cell(2, 3).Value = "Planned";

            worksheet.Cell(3, 1).Value = 11;
            worksheet.Cell(3, 2).Value = "Imported Expansion";
            worksheet.Cell(3, 3).Value = "Playing";
            worksheet.Cell(3, 4).Value = 10;
            worksheet.Cell(3, 5).Value = "Bring snacks";

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        private static void AddWorksheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers, IReadOnlyList<object?> values)
        {
            var worksheet = workbook.Worksheets.Add(name);
            for (var index = 0; index < headers.Count; index++)
            {
                worksheet.Cell(1, index + 1).Value = headers[index];
                worksheet.Cell(2, index + 1).Value = values[index] switch
                {
                    null => Blank.Value,
                    DateTime date => date,
                    _ => XLCellValue.FromObject(values[index])
                };
            }
        }

        private static Dictionary<string, int> HeaderMap(IXLWorksheet worksheet)
        {
            return worksheet.Row(1)
                .CellsUsed()
                .ToDictionary(cell => cell.GetString(), cell => cell.Address.ColumnNumber);
        }

        private sealed class TestDbContextFactory : IDbContextFactory<LuminaPathDbContext>
        {
            private readonly DbContextOptions<LuminaPathDbContext> _options;

            public TestDbContextFactory(DbContextOptions<LuminaPathDbContext> options)
            {
                _options = options;
            }

            public LuminaPathDbContext CreateDbContext()
            {
                return new LuminaPathDbContext(_options);
            }

            public ValueTask<LuminaPathDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return new ValueTask<LuminaPathDbContext>(CreateDbContext());
            }
        }
    }
}
