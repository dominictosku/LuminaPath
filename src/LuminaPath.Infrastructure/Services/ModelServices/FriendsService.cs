using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
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

        public async Task<Result<FriendProfileDto, FailedResult>> GetFriendProfileAsync(
            string viewerId,
            string friendUserId,
            int limit = 6)
        {
            if (string.IsNullOrWhiteSpace(friendUserId) || viewerId == friendUserId)
            {
                return new FailedResult("Friend profile not found.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var friendshipExists = await dbContext.Friendships
                .AsNoTracking()
                .AnyAsync(friendship => friendship.Status == FriendshipStatus.Accepted
                    && ((friendship.RequesterId == viewerId && friendship.AddresseeId == friendUserId)
                        || (friendship.RequesterId == friendUserId && friendship.AddresseeId == viewerId)));

            if (!friendshipExists)
            {
                return new FailedResult("Friend profile not found.");
            }

            var friend = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == friendUserId);

            if (friend == null)
            {
                return new FailedResult("Friend profile not found.");
            }

            limit = Math.Clamp(limit, 1, 12);
            var items = await LoadLibraryItemsAsync(dbContext, friendUserId);
            var completed = items
                .Where(IsCompleted)
                .OrderByDescending(item => item.EndDate ?? DateTime.MinValue)
                .ThenBy(item => item.Title)
                .Take(limit)
                .ToList();
            var active = items
                .Where(IsActive)
                .OrderBy(item => item.Title)
                .Take(limit)
                .ToList();
            var activity = BuildActivity(items, limit);

            return new FriendProfileDto
            {
                User = new FriendUserDto
                {
                    Id = friend.Id,
                    UserName = friend.UserName ?? string.Empty,
                    FullName = friend.FullName,
                    Email = friend.Email ?? string.Empty
                },
                Stats = BuildStats(items),
                NowPlaying = active,
                RecentCompletions = completed,
                Activity = activity
            };
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

        private static async Task<List<FriendLibraryItemDto>> LoadLibraryItemsAsync(
            LuminaPathDbContext dbContext,
            string userId)
        {
            var games = await dbContext.MyGames
                .AsNoTracking()
                .Include(myGame => myGame.Game)
                    .ThenInclude(game => game!.Image)
                .Where(myGame => myGame.LuminaUserId == userId)
                .ToListAsync();

            var animes = await dbContext.MyAnimes
                .AsNoTracking()
                .Include(myAnime => myAnime.Anime)
                    .ThenInclude(anime => anime!.Image)
                .Where(myAnime => myAnime.LuminaUserId == userId)
                .ToListAsync();

            var movies = await dbContext.MyMovies
                .AsNoTracking()
                .Include(myMovie => myMovie.Movie)
                    .ThenInclude(movie => movie!.Image)
                .Where(myMovie => myMovie.LuminaUserId == userId)
                .ToListAsync();

            var series = await dbContext.MySeries
                .AsNoTracking()
                .Include(mySeries => mySeries.Series)
                    .ThenInclude(series => series!.Image)
                .Where(mySeries => mySeries.LuminaUserId == userId)
                .ToListAsync();

            return games.Select(MapGameItem)
                .Concat(animes.Select(MapAnimeItem))
                .Concat(movies.Select(MapMovieItem))
                .Concat(series.Select(MapSeriesItem))
                .ToList();
        }

        private static FriendProfileStatsDto BuildStats(IReadOnlyCollection<FriendLibraryItemDto> items)
        {
            var ratings = items
                .Where(item => item.Rating.HasValue)
                .Select(item => (double)item.Rating!.Value)
                .ToList();

            return new FriendProfileStatsDto
            {
                Games = items.Count(item => item.Kind == "games"),
                Animes = items.Count(item => item.Kind == "animes"),
                Movies = items.Count(item => item.Kind == "movies"),
                Series = items.Count(item => item.Kind == "series"),
                TotalItems = items.Count,
                CompletedItems = items.Count(IsCompleted),
                ActiveItems = items.Count(IsActive),
                TotalTrackedHours = Math.Round(items.Sum(item => item.TimeSpend ?? 0), 1),
                AverageRating = ratings.Count == 0 ? null : Math.Round(ratings.Average(), 1)
            };
        }

        private static List<FriendActivityItemDto> BuildActivity(
            IReadOnlyCollection<FriendLibraryItemDto> items,
            int limit)
        {
            var completionEvents = items
                .Where(IsCompleted)
                .Select(item => new FriendActivityItemDto
                {
                    Kind = "completion",
                    Verb = item.Kind == "games" && item.Status == GameStatus.StoryComplete.ToString()
                        ? "finished the story"
                        : "completed",
                    OccurredAt = item.EndDate,
                    Item = item
                });

            var ratingEvents = items
                .Where(item => item.Rating.HasValue)
                .Select(item => new FriendActivityItemDto
                {
                    Kind = "rating",
                    Verb = $"rated {item.Rating}/10",
                    OccurredAt = item.EndDate ?? item.StartDate,
                    Item = item
                });

            return completionEvents
                .Concat(ratingEvents)
                .OrderByDescending(activity => activity.OccurredAt ?? DateTime.MinValue)
                .ThenBy(activity => activity.Item.Title)
                .Take(limit)
                .ToList();
        }

        private static bool IsCompleted(FriendLibraryItemDto item)
        {
            return item.Kind == "games"
                ? item.Status is nameof(GameStatus.Completed) or nameof(GameStatus.StoryComplete)
                : item.Status == nameof(MediaStatus.Completed);
        }

        private static bool IsActive(FriendLibraryItemDto item)
        {
            return item.Kind == "games"
                ? item.Status == nameof(GameStatus.Playing)
                : item.Status == nameof(MediaStatus.Watching);
        }

        private static FriendLibraryItemDto MapGameItem(MyGame item)
        {
            return MapItem(
                kind: "games",
                mediaId: item.GameId,
                libraryEntryId: item.Id,
                title: item.Game?.Name ?? "Untitled game",
                status: item.Status.ToString(),
                rating: item.Rating,
                timeSpend: item.TimeSpend,
                startDate: item.StartDate,
                endDate: item.EndDate,
                image: item.Game?.Image);
        }

        private static FriendLibraryItemDto MapAnimeItem(MyAnime item)
        {
            return MapItem(
                kind: "animes",
                mediaId: item.AnimeId,
                libraryEntryId: item.Id,
                title: item.Anime?.Name ?? "Untitled anime",
                status: item.Status.ToString(),
                rating: item.Rating,
                timeSpend: item.TimeSpend,
                startDate: item.StartDate,
                endDate: item.EndDate,
                image: item.Anime?.Image);
        }

        private static FriendLibraryItemDto MapMovieItem(MyMovie item)
        {
            return MapItem(
                kind: "movies",
                mediaId: item.MovieId,
                libraryEntryId: item.Id,
                title: item.Movie?.Name ?? "Untitled movie",
                status: item.Status.ToString(),
                rating: item.Rating,
                timeSpend: item.TimeSpend,
                startDate: item.StartDate,
                endDate: item.EndDate,
                image: item.Movie?.Image);
        }

        private static FriendLibraryItemDto MapSeriesItem(MySeries item)
        {
            return MapItem(
                kind: "series",
                mediaId: item.SeriesId,
                libraryEntryId: item.Id,
                title: item.Series?.Name ?? "Untitled series",
                status: item.Status.ToString(),
                rating: item.Rating,
                timeSpend: item.TimeSpend,
                startDate: item.StartDate,
                endDate: item.EndDate,
                image: item.Series?.Image);
        }

        private static FriendLibraryItemDto MapItem(
            string kind,
            int mediaId,
            int libraryEntryId,
            string title,
            string status,
            short? rating,
            double? timeSpend,
            DateTime? startDate,
            DateTime? endDate,
            Document? image)
        {
            return new FriendLibraryItemDto
            {
                Kind = kind,
                MediaId = mediaId,
                LibraryEntryId = libraryEntryId,
                Title = title,
                Status = status,
                Rating = rating,
                TimeSpend = timeSpend,
                StartDate = startDate,
                EndDate = endDate,
                ImageUrl = image?.Url
            };
        }
    }
}
