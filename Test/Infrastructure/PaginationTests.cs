using LuminaPath.Core.Entities;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;

namespace Test.Infrastructure
{
    public class PaginationTests
    {
        [Fact]
        public void Create_ClampsInvalidPagingValues()
        {
            var page = PaginatedList<int>.Create([1, 2, 3], 0, 0);

            Assert.Equal(1, page.PageIndex);
            Assert.Single(page);
            Assert.Equal(1, page[0]);
        }

        [Fact]
        public async Task GenericModelService_UsesRequestedPage_WhenPageSizeIsSet()
        {
            var options = Utilities.DbContext.TestDbContextOptions();

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Games.AddRange(
                    CreateGame("Apex"),
                    CreateGame("Baldur"),
                    CreateGame("Celeste"),
                    CreateGame("Doom"),
                    CreateGame("Elden Ring"));
                await dbContext.SaveChangesAsync();
            }

            var service = CreateGameService(options);
            var mediaFilter = new MediaFilter { Paging = new Paging(pageIndex: 2, count: 2) };

            var page = await service.GetAllPaginated(
                mediaFilter,
                includes: [],
                filter: null,
                orderBy: games => games.OrderBy(game => game.Id));

            Assert.Equal(2, page.PageIndex);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(["Celeste", "Doom"], page.Select(game => game.Name));
        }

        private static GameService CreateGameService(DbContextOptions<LuminaPathDbContext> options)
        {
            var storage = new Mock<LuminaPath.Core.Interfaces.IStorageService>().Object;
            var documentService = new DocumentService(
                new TestDbContextFactory(options),
                storage,
                new Mock<ILogger<DocumentService>>().Object);

            return new GameService(new TestDbContextFactory(options), documentService, new ObjectMapper());
        }

        private static Game CreateGame(string name)
        {
            return new Game
            {
                Name = name,
                Description = $"{name} description"
            };
        }
    }
}
