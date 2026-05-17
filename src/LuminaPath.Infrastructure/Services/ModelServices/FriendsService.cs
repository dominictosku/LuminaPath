using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class FriendsService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly Func<DateTime> _utcNow;

        public FriendsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
            : this(dbContextFactory, () => DateTime.UtcNow)
        {
        }

        public FriendsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, Func<DateTime> utcNow)
        {
            _dbContextFactory = dbContextFactory;
            _utcNow = utcNow;
        }

        private DateTime UtcNow => _utcNow();

        public async Task<List<FriendshipDto>> GetForUserAsync(string userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var rows = await dbContext.Friendships
                .AsNoTracking()
                .Where(friendship => friendship.RequesterId == userId || friendship.AddresseeId == userId)
                .OrderByDescending(friendship => friendship.CreatedAt)
                .ToListAsync();

            var users = await LoadUsersForFriendships(dbContext, rows);
            return rows.Select(friendship => MapToDto(friendship, users, userId)).ToList();
        }

        public async Task<List<FriendUserDto>> SearchUsersAsync(string currentUserId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<FriendUserDto>();
            }
            var normalized = query.Trim().ToLowerInvariant();

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id != currentUserId
                    && ((user.UserName != null && user.UserName.ToLower().Contains(normalized))
                        || (user.Email != null && user.Email.ToLower().Contains(normalized))
                        || user.FullName.ToLower().Contains(normalized)))
                .OrderBy(user => user.UserName)
                .Take(20)
                .Select(user => new FriendUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty
                })
                .ToListAsync();
        }

        public async Task<Result<FriendshipDto, FailedResult>> SendRequestAsync(string requesterId, string addresseeId)
        {
            if (string.IsNullOrWhiteSpace(addresseeId) || requesterId == addresseeId)
            {
                return new FailedResult("Invalid friend target.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var addresseeExists = await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == addresseeId);
            if (!addresseeExists)
            {
                return new FailedResult("User not found.");
            }

            var existing = await dbContext.Friendships
                .FirstOrDefaultAsync(friendship =>
                    (friendship.RequesterId == requesterId && friendship.AddresseeId == addresseeId)
                    || (friendship.RequesterId == addresseeId && friendship.AddresseeId == requesterId));

            if (existing != null)
            {
                if (existing.Status == FriendshipStatus.Accepted)
                {
                    return new FailedResult("You are already friends.");
                }
                if (existing.Status == FriendshipStatus.Pending)
                {
                    if (existing.AddresseeId == requesterId)
                    {
                        existing.Status = FriendshipStatus.Accepted;
                        existing.RespondedAt = UtcNow;
                        await dbContext.SaveChangesAsync();
                        return await LoadDto(dbContext, existing.Id, requesterId);
                    }
                    return new FailedResult("Friend request already pending.");
                }
                existing.Status = FriendshipStatus.Pending;
                existing.RequesterId = requesterId;
                existing.AddresseeId = addresseeId;
                existing.CreatedAt = UtcNow;
                existing.RespondedAt = null;
                await dbContext.SaveChangesAsync();
                return await LoadDto(dbContext, existing.Id, requesterId);
            }

            var friendship = new Friendship
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = FriendshipStatus.Pending,
                CreatedAt = UtcNow
            };
            await dbContext.Friendships.AddAsync(friendship);
            await dbContext.SaveChangesAsync();
            return await LoadDto(dbContext, friendship.Id, requesterId);
        }

        public async Task<Result<FriendshipDto, FailedResult>> RespondAsync(string userId, int friendshipId, bool accept)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var friendship = await dbContext.Friendships
                .FirstOrDefaultAsync(row => row.Id == friendshipId);

            if (friendship == null || friendship.AddresseeId != userId)
            {
                return new FailedResult("Friend request not found.");
            }
            if (friendship.Status != FriendshipStatus.Pending)
            {
                return new FailedResult("Request already handled.");
            }

            friendship.Status = accept ? FriendshipStatus.Accepted : FriendshipStatus.Declined;
            friendship.RespondedAt = UtcNow;
            await dbContext.SaveChangesAsync();
            return await LoadDto(dbContext, friendship.Id, userId);
        }

        public async Task<Result<int, FailedResult>> RemoveAsync(string userId, int friendshipId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var friendship = await dbContext.Friendships
                .FirstOrDefaultAsync(row => row.Id == friendshipId
                    && (row.RequesterId == userId || row.AddresseeId == userId));

            if (friendship == null)
            {
                return new FailedResult("Friendship not found.");
            }

            dbContext.Friendships.Remove(friendship);
            await dbContext.SaveChangesAsync();
            return friendshipId;
        }

        public async Task<bool> AreFriendsAsync(string userA, string userB)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await dbContext.Friendships
                .AsNoTracking()
                .AnyAsync(friendship => friendship.Status == FriendshipStatus.Accepted
                    && ((friendship.RequesterId == userA && friendship.AddresseeId == userB)
                        || (friendship.RequesterId == userB && friendship.AddresseeId == userA)));
        }

        private static async Task<FriendshipDto> LoadDto(LuminaPathDbContext dbContext, int id, string viewerId)
        {
            var friendship = await dbContext.Friendships
                .AsNoTracking()
                .FirstAsync(row => row.Id == id);
            var users = await LoadUsersForFriendships(dbContext, new[] { friendship });
            return MapToDto(friendship, users, viewerId);
        }

        private static async Task<Dictionary<string, LuminaUser>> LoadUsersForFriendships(
            LuminaPathDbContext dbContext, IEnumerable<Friendship> friendships)
        {
            var userIds = friendships
                .SelectMany(f => new[] { f.RequesterId, f.AddresseeId })
                .Distinct()
                .ToList();

            return await dbContext.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id);
        }

        private static FriendshipDto MapToDto(Friendship friendship, Dictionary<string, LuminaUser> users, string viewerId)
        {
            var isIncoming = friendship.AddresseeId == viewerId;
            var otherId = isIncoming ? friendship.RequesterId : friendship.AddresseeId;
            users.TryGetValue(otherId, out var other);
            return new FriendshipDto
            {
                Id = friendship.Id,
                Status = friendship.Status.ToString(),
                IsIncoming = isIncoming,
                CreatedAt = friendship.CreatedAt,
                RespondedAt = friendship.RespondedAt,
                User = new FriendUserDto
                {
                    Id = other?.Id ?? string.Empty,
                    UserName = other?.UserName ?? string.Empty,
                    FullName = other?.FullName ?? string.Empty,
                    Email = other?.Email ?? string.Empty
                }
            };
        }
    }
}
