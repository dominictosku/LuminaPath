using LuminaPath.Core.Enums;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
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
                    LuminaUser = user,
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
    }
}
