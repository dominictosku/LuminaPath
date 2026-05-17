using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;

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

        [Fact]
        public async Task PreviewGamesAsync_MarksDuplicateRows_FromWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateWorkbookStream(includeDuplicate: true);

            var preview = await service.PreviewGamesAsync(stream, user, "games.xlsx");

            Assert.Equal(2, preview.RowsDetected);
            Assert.Equal(1, preview.DuplicateRows);
            Assert.Contains(preview.Rows, row => row.ChangeType == "New");
            Assert.Contains(preview.Rows, row => row.ChangeType == "Duplicate");
        }

        [Fact]
        public async Task ImportGamesAsync_SkipsDuplicateRows_FromWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateWorkbookStream(includeDuplicate: true);

            var result = await service.ImportGamesAsync(stream, user, "games.xlsx");

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, result.RowsImported);
            Assert.Single(await assertContext.Games.ToListAsync());
            Assert.Contains(result.Errors, error => error.Contains("duplicate import row skipped", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task PreviewGamesAsync_ReadsOdsWorkbook()
        {
            var options = CreateOptions();
            var user = await SeedUser(options);
            var service = CreateService(options);
            using var stream = CreateOdsStream();

            var preview = await service.PreviewGamesAsync(stream, user, "games.ods");

            var row = Assert.Single(preview.Rows);
            Assert.Equal("ODS Game", row.Name);
            Assert.Equal("New", row.ChangeType);
            Assert.Equal("PSN-ODS", row.PsnId);
            Assert.Equal(3.25, row.TrackedHours);
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

        private static MemoryStream CreateOdsStream()
        {
            const string content = """
                <?xml version="1.0" encoding="UTF-8"?>
                <office:document-content
                    xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                    xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0"
                    xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">
                    <office:body>
                        <office:spreadsheet>
                            <table:table table:name="Games">
                                <table:table-row>
                                    <table:table-cell><text:p>Name</text:p></table:table-cell>
                                    <table:table-cell><text:p>Status</text:p></table:table-cell>
                                    <table:table-cell><text:p>PSNId</text:p></table:table-cell>
                                    <table:table-cell><text:p>Tracked Hours</text:p></table:table-cell>
                                </table:table-row>
                                <table:table-row>
                                    <table:table-cell><text:p>ODS Game</text:p></table:table-cell>
                                    <table:table-cell><text:p>Playing</text:p></table:table-cell>
                                    <table:table-cell><text:p>PSN-ODS</text:p></table:table-cell>
                                    <table:table-cell office:value-type="float" office:value="3.25"><text:p>3.25</text:p></table:table-cell>
                                </table:table-row>
                            </table:table>
                        </office:spreadsheet>
                    </office:body>
                </office:document-content>
                """;

            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var mimetype = archive.CreateEntry("mimetype");
                using (var writer = new StreamWriter(mimetype.Open(), Encoding.UTF8))
                {
                    writer.Write("application/vnd.oasis.opendocument.spreadsheet");
                }

                var contentEntry = archive.CreateEntry("content.xml");
                using var contentWriter = new StreamWriter(contentEntry.Open(), Encoding.UTF8);
                contentWriter.Write(content);
            }

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
