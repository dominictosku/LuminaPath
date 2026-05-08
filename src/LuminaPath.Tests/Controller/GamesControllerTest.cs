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
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Entities;

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
				var games = Seeding.SeedGames(names);
				db.Games.AddRange(games);
				await db.SaveChangesAsync();

				GamesController controller = new GamesController(gameService, mapper);
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
