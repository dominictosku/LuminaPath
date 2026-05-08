using LuminaPath.Infrastructure;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Third_Party;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Test.Controller
{
    public class GamesControllerTest
	{
		[Fact]
		public async Task GetAsync_GetAllGamesWithMyGames_WhenLoggedIn()
		{
			var dbOptions = Utilities.DbContext.TestDbContextOptions();
			using (var db = new LuminaPathDbContext(dbOptions))
			{
				// Arrange
				string[] names = new string[] {
					"Apex",
					"God of War"
				};
				var filter = new MediaFilter();
				var dbContextFactory = new Mock<IDbContextFactory<LuminaPathDbContext>>();
				dbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
					.ReturnsAsync(() => new LuminaPathDbContext(dbOptions));
				var azure = new Mock<IStorageService>().Object;
				var mapper = db.GetService<IObjectMapper>();
				var documentService = new DocumentService(dbContextFactory.Object, azure, new Mock<ILogger<DocumentService>>().Object);
				var gameService = new GameService(dbContextFactory.Object, documentService, mapper);
				var settingsService = new ApplicationSettingsService(dbContextFactory.Object);
				var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
				var gameNewsService = new GameNewsService(
					new HttpClient(),
					dbContextFactory.Object,
					cache,
					settingsService,
					Options.Create(new GameNewsOptions()),
					new Mock<ILogger<GameNewsService>>().Object);
				var games = Seeding.SeedGames(names);
				db.Games.AddRange(games);
				await db.SaveChangesAsync();

				GamesController controller = new GamesController(gameService, mapper, gameNewsService);
				var expectedGames = await db.Games.ToListAsync();

				// Act
				var actualGames = await controller.Get(filter);
				var okResult = Assert.IsType<OkObjectResult>(actualGames.Result);
				var paginatedResult = Assert.IsType<PaginatedResult<GamesDto>>(okResult.Value);

				// Assert
				Assert.Equal(
					expectedGames.OrderBy(m => m.Id).Select(m => m.Id),
					paginatedResult.Data.OrderBy(g => g.Id).Select(g => g.Id));
			}
		}
	}
}
