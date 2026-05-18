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
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.ThirdParty;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using LuminaPath.Core.Enums;

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

				GamesController controller = new GamesController(gameService, mapper, new NewsAggregationService(gameNewsService));
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

		[Fact]
		public async Task GetAsync_IncludesTrackedHoursForLoggedInUsersLibrary()
		{
			var dbOptions = Utilities.DbContext.TestDbContextOptions();
			const string userId = "user-1";

			await using (var db = new LuminaPathDbContext(dbOptions))
			{
				db.Users.Add(new LuminaPath.Infrastructure.Identity.LuminaUser
				{
					Id = userId,
					UserName = "user-1@example.test",
					Email = "user-1@example.test"
				});
				var game = new Game { Name = "Control", Description = "Action", Playtime = 20 };
				db.Games.Add(game);
				db.MyGames.Add(new MyGame
				{
					Game = game,
					LuminaUserId = userId,
					Status = GameStatus.Playing,
					TimeSpend = 5,
					Priority = 1,
					MyGameInfo = new LuminaPath.Core.Models.ThirdParty.MyGameInfo
					{
						TrackedHours = 7.5
					}
				});
				await db.SaveChangesAsync();
			}

			await using (var db = new LuminaPathDbContext(dbOptions))
			{
				var dbContextFactory = new Mock<IDbContextFactory<LuminaPathDbContext>>();
				dbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
					.ReturnsAsync(() => new LuminaPathDbContext(dbOptions));
				var mapper = db.GetService<IObjectMapper>();
				var documentService = new DocumentService(
					dbContextFactory.Object,
					new Mock<IStorageService>().Object,
					new Mock<ILogger<DocumentService>>().Object);
				var gameNewsService = CreateGameNewsService(dbContextFactory.Object);
				var controller = new GamesController(
					new GameService(dbContextFactory.Object, documentService, mapper),
					mapper,
					new NewsAggregationService(gameNewsService))
				{
					ControllerContext = MockServices.ControllerContextWithUser(userId)
				};

				var filter = new MediaFilter { MyMedia = true };
				var actualGames = await controller.Get(filter);

				var okResult = Assert.IsType<OkObjectResult>(actualGames.Result);
				var paginatedResult = Assert.IsType<PaginatedResult<GamesDto>>(okResult.Value);
				var gameDto = Assert.Single(paginatedResult.Data);
				Assert.NotNull(gameDto.MyGames);
				Assert.Equal(5, gameDto.MyGames!.TimeSpend);
				Assert.Equal(7.5, gameDto.MyGames.MyGameInfo?.TrackedHours);
			}
		}

		private static GameNewsService CreateGameNewsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
		{
			var settingsService = new ApplicationSettingsService(dbContextFactory);
			var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
			return new GameNewsService(
				new HttpClient(),
				dbContextFactory,
				cache,
				settingsService,
				Options.Create(new GameNewsOptions()),
				new Mock<ILogger<GameNewsService>>().Object);
		}
	}
}
