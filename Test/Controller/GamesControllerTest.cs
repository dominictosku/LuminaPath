using Application.Controllers;
using AutoMapper;
using Domain;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Models;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;

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
				var unitOfWork = new UnitOfWork(db, userManager.Object);
				var azure = new Mock<IAzureStorage>().Object;
				var logger = new Mock<ILogger<GamesController>>();
				var mapper = db.GetService<IMapper>();
				var games = Seeding.SeedGames(names);
				db.Games.AddRange(games);
				await db.SaveChangesAsync();

				GamesController controller = new GamesController(unitOfWork, logger.Object, mapper);
				var expectedGames = await db.Games.ToListAsync();

				// Act
				var actualGames = await controller.Get(filter);

				// Assert
				Assert.Equal(
					expectedGames.OrderBy(m => m.Id).Select(m => m.Id),
					actualGames.Data.OrderBy(g => g.Id).Select(g => g.Id));
			}
		}
	}
}