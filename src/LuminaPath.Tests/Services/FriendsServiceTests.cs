using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services;

public class FriendsServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SendRequestAsync_CreatesPendingRequest()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await SeedUsers(options);
        var service = CreateService(options);

        var result = await service.SendRequestAsync("alice", "bob");
        var dto = result.Match(value => value, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.Equal("Pending", dto.Status);
        Assert.False(dto.IsIncoming);
        Assert.Equal("bob", dto.User.Id);

        await using var context = new LuminaPathDbContext(options);
        var friendship = await context.Friendships.SingleAsync();
        Assert.Equal("alice", friendship.RequesterId);
        Assert.Equal("bob", friendship.AddresseeId);
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public async Task SendRequestAsync_AcceptsReversePendingRequest()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await SeedUsers(options);
        await using (var seedContext = new LuminaPathDbContext(options))
        {
            seedContext.Friendships.Add(new Friendship
            {
                RequesterId = "bob",
                AddresseeId = "alice",
                Status = FriendshipStatus.Pending,
                CreatedAt = FixedNow.AddDays(-1)
            });
            await seedContext.SaveChangesAsync();
        }

        var service = CreateService(options);

        var result = await service.SendRequestAsync("alice", "bob");
        var dto = result.Match(value => value, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.Equal("Accepted", dto.Status);
        Assert.True(dto.IsIncoming);
        Assert.Equal(FixedNow, dto.RespondedAt);

        await using var context = new LuminaPathDbContext(options);
        var friendship = await context.Friendships.SingleAsync();
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        Assert.Equal(FixedNow, friendship.RespondedAt);
    }

    [Fact]
    public async Task RespondAsync_RejectsNonAddressee()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await SeedUsers(options);
        await using (var seedContext = new LuminaPathDbContext(options))
        {
            seedContext.Friendships.Add(new Friendship
            {
                RequesterId = "alice",
                AddresseeId = "bob",
                Status = FriendshipStatus.Pending,
                CreatedAt = FixedNow
            });
            await seedContext.SaveChangesAsync();
        }

        var service = CreateService(options);

        var result = await service.RespondAsync("alice", 1, accept: true);

        Assert.True(result.IsError);
        var message = result.Match(_ => string.Empty, failure => string.Join("; ", failure.errorMessage));
        Assert.Equal("Friend request not found.", message);
    }

    [Fact]
    public async Task SearchUsersAsync_ExcludesCurrentUserAndMatchesNameOrEmail()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await SeedUsers(options);
        var service = CreateService(options);

        var users = await service.SearchUsersAsync("alice", "bo");

        var user = Assert.Single(users);
        Assert.Equal("bob", user.Id);
        Assert.Equal("bob@example.test", user.Email);
    }

    private static FriendsService CreateService(DbContextOptions<LuminaPathDbContext> options)
    {
        return new FriendsService(new TestDbContextFactory(options), () => FixedNow);
    }

    private static async Task SeedUsers(DbContextOptions<LuminaPathDbContext> options)
    {
        await using var context = new LuminaPathDbContext(options);
        context.Users.AddRange(
            new LuminaUser
            {
                Id = "alice",
                UserName = "alice@example.test",
                Email = "alice@example.test",
                FullName = "Alice Admin"
            },
            new LuminaUser
            {
                Id = "bob",
                UserName = "bob@example.test",
                Email = "bob@example.test",
                FullName = "Bob Builder"
            },
            new LuminaUser
            {
                Id = "cora",
                UserName = "cora@example.test",
                Email = "cora@example.test",
                FullName = "Cora Viewer"
            });
        await context.SaveChangesAsync();
    }
}
