using AutoMapper;
using Data;
using Data.Classes;
using Data.Models;
using Data.Repositories;
using Data.Services;
using Google.Protobuf.WellKnownTypes;
using LuminaPath.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
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
				var filter = new Paging();
				var gameRepo = new GameRepo(db);
				var logger = new Mock<ILogger<GamesController>>();
				var mapper = db.GetService<IMapper>();
				var games = Seeding.SeedGames(names);
				db.Games.AddRange(games);
				await db.SaveChangesAsync();

				GamesController controller = new GamesController(gameRepo, logger.Object, mapper);
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