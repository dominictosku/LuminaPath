using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Controller
{
    public class MyGamesControllerTest
    {
        [Fact]
        public async Task PostAsync_ReturnsCreated_WhenGameIsAddedToLibrary()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int gameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Witcher 3", Description = "RPG" };
                dbContext.Games.Add(game);
                await dbContext.SaveChangesAsync();
                gameId = game.Id;
            }

            var (controller, _) = CreateController(options, userId);

            var result = await controller.PostAsync(new MyGameDto
            {
                GameId = gameId,
                Status = GameStatus.Planned
            });

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var dto = Assert.IsType<MyGameDto>(created.Value);
            Assert.Equal(gameId, dto.GameId);

            await using var assertContext = new LuminaPathDbContext(options);
            var saved = await assertContext.MyGames.SingleAsync();
            Assert.Equal(userId, saved.LuminaUserId);
            Assert.Equal(gameId, saved.GameId);
        }

        [Fact]
        public async Task PostAsync_ReturnsBadRequest_WhenGameIsAlreadyInLibrary()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int gameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Doom Eternal", Description = "FPS" };
                dbContext.Games.Add(game);
                dbContext.MyGames.Add(new MyGame
                {
                    Game = game,
                    LuminaUserId = userId,
                    Status = GameStatus.Playing,
                    Priority = 1
                });
                await dbContext.SaveChangesAsync();
                gameId = game.Id;
            }

            var (controller, _) = CreateController(options, userId);

            var result = await controller.PostAsync(new MyGameDto
            {
                GameId = gameId,
                Status = GameStatus.Planned
            });

            Assert.IsType<BadRequestObjectResult>(result);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, await assertContext.MyGames.CountAsync());
        }

        [Fact]
        public async Task PutAsync_ReturnsBadRequest_WhenIdDoesNotMatchEntity()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var (controller, _) = CreateController(options, "user-1");

            var result = await controller.PutAsync(1, new MyGameDto { Id = 2, GameId = 1 });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutAsync_NullsOutMyGameInfo_BeforePersisting()
        {
            // The MyGamesController.PutAsync override clears MyGameInfo on the incoming dto so
            // clients cannot mutate tracking data via the library update endpoint. If this guard
            // is ever removed, the test catches it.
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;
            int gameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Hollow Knight", Description = "Metroidvania" };
                dbContext.Games.Add(game);
                var myGame = new MyGame
                {
                    Game = game,
                    LuminaUserId = userId,
                    Status = GameStatus.Planned,
                    Priority = 1
                };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;
                gameId = game.Id;
            }

            var (controller, _) = CreateController(options, userId);

            var dto = new MyGameDto
            {
                Id = myGameId,
                GameId = gameId,
                Status = GameStatus.Playing,
                MyGameInfo = new LuminaPath.Core.Models.Third_Party.MyGameInfo
                {
                    TrackedHours = 999,
                    FirstPlayed = new DateTime(1999, 1, 1),
                    LastPlayed = new DateTime(1999, 12, 31)
                }
            };

            var result = await controller.PutAsync(myGameId, dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(dto.MyGameInfo);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsOk_WhenEntryIsDeleted()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Portal 2", Description = "Puzzle" };
                dbContext.Games.Add(game);
                var myGame = new MyGame
                {
                    Game = game,
                    LuminaUserId = userId,
                    Status = GameStatus.Completed,
                    Priority = 1
                };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;
            }

            var (controller, _) = CreateController(options, userId);

            var result = await controller.DeleteAsync(myGameId);

            Assert.IsType<OkResult>(result);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(0, await assertContext.MyGames.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenEntryDoesNotExist()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var (controller, _) = CreateController(options, "user-1");

            var result = await controller.DeleteAsync(9999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        private static (MyGamesController controller, MyGameService service) CreateController(
            DbContextOptions<LuminaPathDbContext> options, string userId)
        {
            var user = NewUser(userId);
            var userManager = MockServices.UserManagerMock(user);
            var mapper = new ObjectMapper();
            var service = new MyGameService(new TestDbContextFactory(options), mapper);

            var controller = new MyGamesController(service, userManager.Object, mapper)
            {
                ControllerContext = MockServices.ControllerContextWithUser(userId)
            };

            return (controller, service);
        }

        private static LuminaUser NewUser(string id) => new()
        {
            Id = id,
            UserName = $"{id}@example.test",
            Email = $"{id}@example.test"
        };
    }
}
