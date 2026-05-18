using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Imports;
using LuminaPath.Infrastructure.Services.ThirdParty;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Services
{
    public class PsnImportServiceTests
    {
        [Fact]
        public async Task PreviewGames_MarksExistingPsnLibraryEntry_AsUpdated()
        {
            var options = CreateOptions();
            var user = new LuminaUser { Id = "user-1", UserName = "test@example.com" };

            await using (var context = new LuminaPathDbContext(options))
            {
                context.Users.Add(user);
                var game = new Game
                {
                    Name = "Astro Bot",
                    Source = "PSN",
                    ExternalIds = [new MediaExternalId { Provider = ExternalMediaProvider.Psn, ExternalId = "NPWR-123" }]
                };
                context.Games.Add(game);
                context.MyGames.Add(new MyGame
                {
                    Game = game,
                    LuminaUserId = user.Id,
                    Status = GameStatus.Playing
                });
                await context.SaveChangesAsync();
            }

            var service = CreateService(options);
            var preview = await service.PreviewGames(user,
            [
                new MyGameDto
                {
                    Game = new GamesNoIncludeDto
                    {
                        Name = "Astro Bot",
                        Source = "PSN",
                        PsnId = "NPWR-123"
                    },
                    Status = GameStatus.Playing
                }
            ]);

            var row = Assert.Single(preview.Rows);
            Assert.Equal("Updated", row.ChangeType);
            Assert.Equal("Update", row.GameAction);
            Assert.Equal("Update", row.LibraryAction);
        }

        private static DbContextOptions<LuminaPathDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<LuminaPathDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static PSNService CreateService(DbContextOptions<LuminaPathDbContext> options)
        {
            var dbContextFactory = new TestDbContextFactory(options);
            return new PSNService(
                new HttpClient(),
                new GameImportPipeline(dbContextFactory),
                new Mock<ILogger<PSNService>>().Object);
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
