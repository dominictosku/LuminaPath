using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;

namespace Test.Services
{
    public class ExcelServiceTests
    {
        [Fact]
        public async Task ImportGamesAsync_CreatesGameAndMyGame_FromWorkbook()
        {
            var options = CreateOptions();
            var user = new LuminaUser
            {
                Id = "user-1",
                UserName = "test@example.com",
                FullName = "Test User"
            };

            await using (var context = new LuminaPathDbContext(options))
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            var dbContextFactory = new TestDbContextFactory(options);
            var service = new ExcelService(dbContextFactory, new GameImportPipeline(dbContextFactory));
            using var stream = CreateWorkbookStream();
            var result = await service.ImportGamesAsync(stream, user);
            await using var assertContext = new LuminaPathDbContext(options);
            var game = await assertContext.Games
                .Include(g => g.ExternalIds)
                .Include(g => g.MyGames!)
                    .ThenInclude(g => g.MyGameInfo)
                .SingleAsync();
            var myGame = game.MyGames!.Single();
            Assert.Empty(result.Errors);
            Assert.Equal(1, result.RowsImported);
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

        private static DbContextOptions<LuminaPathDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<LuminaPathDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static MemoryStream CreateWorkbookStream()
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

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
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
