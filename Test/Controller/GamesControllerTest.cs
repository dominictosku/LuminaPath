using AutoMapper;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Infrastructure;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Controllers;

namespace Test.Controller
{
	public class GamesControllerTest
	{
		[Fact]
		public async Task GetAsync_GetAllGamesWithMyGames_WhenLoggedIn()
		{
			using (var db = new LuminaPathDbContext(Utilities.DbContext.TestDbContextOptions()))
			{
				// Arrange
				string[] names = new string[] {
					"Apex",
					"God of War"
				};
				var filter = new MediaFilter();
				var userManager = new Mock<UserManager<LuminaUser>>();
				var gameService = new Mock<GameService>();
				var azure = new Mock<IStorageService>().Object;
				var logger = new Mock<ILogger<GamesController>>();
				var mapper = db.GetService<IMapper>();
				var games = Seeding.SeedGames(names);
				db.Games.AddRange(games);
				await db.SaveChangesAsync();

				GamesController controller = new GamesController(gameService.Object, logger.Object, mapper);
				var expectedGames = await db.Games.ToListAsync();

				// Act
				var actualGames = await controller.Get(filter);

				// Assert
				Assert.Equal(
					expectedGames.OrderBy(m => m.Id).Select(m => m.Id),
					actualGames.Value.Data.OrderBy(g => g.Id).Select(g => g.Id));
			}
		}
	}
}