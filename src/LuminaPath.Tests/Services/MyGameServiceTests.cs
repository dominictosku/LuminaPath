using LuminaPath.Core.Entities;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services
{
    public class MyGameServiceTests
    {
        [Fact]
        public async Task PutAsync_UpdatesExistingMyGame_WhenMediaIdIsComputed()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                var user = new LuminaUser
                {
                    Id = userId,
                    UserName = "test@example.com",
                    Email = "test@example.com"
                };
                var game = new Game
                {
                    Name = "Celeste",
                    Description = "Platformer"
                };

                dbContext.Users.Add(user);
                dbContext.Games.Add(game);
                dbContext.MyGames.Add(new MyGame
                {
                    Game = game,
                    LuminaUserId = userId,
                    Status = GameStatus.Planned,
                    Priority = 1
                });
                await dbContext.SaveChangesAsync();
            }

            MyGame existing;
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                existing = await dbContext.MyGames
                    .AsNoTracking()
                    .SingleAsync();
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());
            existing.Status = GameStatus.Playing;
            existing.Priority = 2;

            var result = await service.PutAsync(existing, new LuminaUser { Id = userId });

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            var updated = await assertContext.MyGames.SingleAsync();
            Assert.Equal(GameStatus.Playing, updated.Status);
            Assert.Equal(2, updated.Priority);
        }

        [Fact]
        public async Task PostAsync_AddsGameToLibrary_WhenUserIsValid()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int gameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Hades", Description = "Roguelike" };
                dbContext.Games.Add(game);
                await dbContext.SaveChangesAsync();
                gameId = game.Id;
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.PostAsync(
                new MyGame { GameId = gameId, Status = GameStatus.Planned, Priority = 1 },
                new LuminaUser { Id = userId });

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            var saved = await assertContext.MyGames.SingleAsync();
            Assert.Equal(userId, saved.LuminaUserId);
            Assert.Equal(gameId, saved.GameId);
            Assert.Equal(GameStatus.Planned, saved.Status);
        }

        [Fact]
        public async Task PostAsync_ReturnsFailure_WhenUserIsNull()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.PostAsync(
                new MyGame { GameId = 1, Status = GameStatus.Planned, Priority = 1 },
                user: null);

            Assert.False(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(0, await assertContext.MyGames.CountAsync());
        }

        [Fact]
        public async Task PostAsync_ReturnsFailure_WhenSameGameAlreadyInLibraryForUser()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int gameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Stardew Valley", Description = "Farm" };
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

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.PostAsync(
                new MyGame { GameId = gameId, Status = GameStatus.Planned, Priority = 1 },
                new LuminaUser { Id = userId });

            Assert.False(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, await assertContext.MyGames.CountAsync());
        }

        [Fact]
        public async Task PostAsync_AllowsSameGame_ForDifferentUsers()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userA = "user-a";
            const string userB = "user-b";
            int gameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userA), NewUser(userB));
                var game = new Game { Name = "Tetris", Description = "Puzzle" };
                dbContext.Games.Add(game);
                dbContext.MyGames.Add(new MyGame
                {
                    Game = game,
                    LuminaUserId = userA,
                    Status = GameStatus.Planned,
                    Priority = 1
                });
                await dbContext.SaveChangesAsync();
                gameId = game.Id;
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.PostAsync(
                new MyGame { GameId = gameId, Status = GameStatus.Planned, Priority = 1 },
                new LuminaUser { Id = userB });

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(2, await assertContext.MyGames.CountAsync());
            Assert.Equal(
                new[] { userA, userB }.OrderBy(id => id),
                (await assertContext.MyGames.Select(m => m.LuminaUserId).ToListAsync()).OrderBy(id => id));
        }

        [Fact]
        public async Task PutAsync_ReturnsFailure_WhenChangedToGameAlreadyOwned()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int firstGameId;
            int secondGameId;
            int secondMyGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var first = new Game { Name = "Halo", Description = "FPS" };
                var second = new Game { Name = "Mass Effect", Description = "RPG" };
                dbContext.Games.AddRange(first, second);
                dbContext.MyGames.AddRange(
                    new MyGame { Game = first, LuminaUserId = userId, Status = GameStatus.Playing, Priority = 1 },
                    new MyGame { Game = second, LuminaUserId = userId, Status = GameStatus.Planned, Priority = 1 });
                await dbContext.SaveChangesAsync();
                firstGameId = first.Id;
                secondGameId = second.Id;
                secondMyGameId = (await dbContext.MyGames.SingleAsync(m => m.GameId == secondGameId)).Id;
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.PutAsync(
                new MyGame
                {
                    Id = secondMyGameId,
                    GameId = firstGameId,
                    Status = GameStatus.Playing,
                    Priority = 1
                },
                new LuminaUser { Id = userId });

            Assert.False(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            var unchanged = await assertContext.MyGames.SingleAsync(m => m.Id == secondMyGameId);
            Assert.Equal(secondGameId, unchanged.GameId);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntry_WhenIdExists()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Outer Wilds", Description = "Exploration" };
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

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.DeleteAsync(myGameId);

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(0, await assertContext.MyGames.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFailure_WhenIdIsNull()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.DeleteAsync(null);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFailure_WhenIdNotFound()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var result = await service.DeleteAsync(9999);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteMyData_OnlyDeletesCallingUsersGames()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userA = "user-a";
            const string userB = "user-b";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userA), NewUser(userB));
                var aGame = new Game { Name = "A-Game", Description = "" };
                var bGame = new Game { Name = "B-Game", Description = "" };
                dbContext.Games.AddRange(aGame, bGame);
                dbContext.MyGames.AddRange(
                    new MyGame { Game = aGame, LuminaUserId = userA, Status = GameStatus.Planned, Priority = 1 },
                    new MyGame { Game = bGame, LuminaUserId = userB, Status = GameStatus.Planned, Priority = 1 });
                await dbContext.SaveChangesAsync();
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            await service.DeleteMyData(new LuminaUser { Id = userA });

            await using var assertContext = new LuminaPathDbContext(options);
            var remaining = await assertContext.MyGames.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(userB, remaining[0].LuminaUserId);
        }

        [Fact]
        public async Task GetAllPaginated_OnlyReturnsCallingUsersGames()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userA = "user-a";
            const string userB = "user-b";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userA), NewUser(userB));
                var aGame = new Game { Name = "A-Game", Description = "", ReleaseDate = new DateTime(2020, 1, 1) };
                var bGame = new Game { Name = "B-Game", Description = "", ReleaseDate = new DateTime(2021, 1, 1) };
                dbContext.Games.AddRange(aGame, bGame);
                dbContext.MyGames.AddRange(
                    new MyGame { Game = aGame, LuminaUserId = userA, Status = GameStatus.Planned, Priority = 1 },
                    new MyGame { Game = bGame, LuminaUserId = userB, Status = GameStatus.Planned, Priority = 1 });
                await dbContext.SaveChangesAsync();
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var page = await service.GetAllPaginated(userA, new MediaFilter());

            var only = Assert.Single(page);
            Assert.Equal(userA, only.LuminaUserId);
        }

        [Fact]
        public async Task ExportAsCSV_OnlyContainsCallingUsersGames()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userA = "user-a";
            const string userB = "user-b";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userA), NewUser(userB));
                var aGame = new Game { Name = "Apex", Description = "", ReleaseDate = new DateTime(2020, 1, 1) };
                var bGame = new Game { Name = "Bastion", Description = "", ReleaseDate = new DateTime(2021, 1, 1) };
                dbContext.Games.AddRange(aGame, bGame);
                dbContext.MyGames.AddRange(
                    new MyGame
                    {
                        Game = aGame,
                        LuminaUserId = userA,
                        Status = GameStatus.Playing,
                        Priority = 1,
                        MyGameInfo = new MyGameInfo
                        {
                            TrackedHours = 12.5,
                            FirstPlayed = new DateTime(2024, 1, 1),
                            LastPlayed = new DateTime(2024, 6, 1)
                        }
                    },
                    new MyGame
                    {
                        Game = bGame,
                        LuminaUserId = userB,
                        Status = GameStatus.Completed,
                        Priority = 1
                    });
                await dbContext.SaveChangesAsync();
            }

            var service = new MyGameService(new TestDbContextFactory(options), new ObjectMapper());

            var bytes = await service.ExportAsCSV(new LuminaUser { Id = userA });
            var csv = System.Text.Encoding.UTF8.GetString(bytes);

            Assert.Contains("Apex", csv);
            Assert.DoesNotContain("Bastion", csv);
        }

        private static LuminaUser NewUser(string id) => new()
        {
            Id = id,
            UserName = $"{id}@example.test",
            Email = $"{id}@example.test"
        };
    }
}
