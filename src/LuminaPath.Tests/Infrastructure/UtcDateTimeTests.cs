using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Test.Infrastructure
{
    public class UtcDateTimeTests
    {
        [Fact]
        public async Task SaveChangesAsync_NormalizesDateTimePropertiesToUtc()
        {
            var options = CreateOptions();
            await using var context = new LuminaPathDbContext(options);
            var releaseDate = new DateTime(2026, 5, 2);

            context.Games.Add(new Game
            {
                Name = $"UTC Test {Guid.NewGuid()}",
                ReleaseDate = releaseDate
            });

            await context.SaveChangesAsync();

            var game = await context.Games.SingleAsync();
            Assert.Equal(DateTimeKind.Utc, game.ReleaseDate!.Value.Kind);
            Assert.Equal(releaseDate, game.ReleaseDate.Value);
        }

        [Fact]
        public async Task SaveChangesAsync_NormalizesDateTimeOffsetPropertiesToUtc()
        {
            var options = CreateOptions();
            await using var context = new LuminaPathDbContext(options);
            var lockoutEnd = new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.FromHours(2));

            context.Users.Add(new LuminaUser
            {
                UserName = "utc@example.test",
                Email = "utc@example.test",
                LockoutEnd = lockoutEnd
            });

            await context.SaveChangesAsync();

            var user = await context.Users.SingleAsync();
            Assert.Equal(TimeSpan.Zero, user.LockoutEnd!.Value.Offset);
            Assert.Equal(lockoutEnd.UtcDateTime, user.LockoutEnd.Value.UtcDateTime);
        }

        private static DbContextOptions<LuminaPathDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<LuminaPathDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }
    }
}
