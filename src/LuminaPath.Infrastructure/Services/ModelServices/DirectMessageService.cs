using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class DirectMessageService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly FriendsService _friendsService;
        private readonly Func<DateTime> _utcNow;

        public DirectMessageService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            FriendsService friendsService)
            : this(dbContextFactory, friendsService, () => DateTime.UtcNow)
        {
        }

        public DirectMessageService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            FriendsService friendsService,
            Func<DateTime> utcNow)
        {
            _dbContextFactory = dbContextFactory;
            _friendsService = friendsService;
            _utcNow = utcNow;
        }

        private DateTime UtcNow => _utcNow();

        public async Task<Result<List<DirectMessageDto>, FailedResult>> GetConversationAsync(
            string userId,
            string otherUserId,
            int take = 100,
            DateTime? before = null)
        {
            if (!await _friendsService.AreFriendsAsync(userId, otherUserId))
            {
                return new FailedResult("You can only chat with friends.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var query = dbContext.DirectMessages
                .AsNoTracking()
                .Where(message =>
                    (message.SenderId == userId && message.RecipientId == otherUserId)
                    || (message.SenderId == otherUserId && message.RecipientId == userId));

            if (before.HasValue)
            {
                query = query.Where(message => message.SentAt < before.Value);
            }

            var rows = await query
                .OrderByDescending(message => message.SentAt)
                .Take(Math.Clamp(take, 1, DirectMessageLimits.MaxConversationTake))
                .ToListAsync();

            rows.Reverse();
            return rows.Select(MapToDto).ToList();
        }

        public async Task<Result<DirectMessageDto, FailedResult>> SendAsync(
            string senderId,
            string recipientId,
            string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new FailedResult("Message cannot be empty.");
            }
            if (content.Length > DirectMessageLimits.MaxMessageCharacters)
            {
                return new FailedResult("Message is too long.");
            }
            if (!await _friendsService.AreFriendsAsync(senderId, recipientId))
            {
                return new FailedResult("You can only chat with friends.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var message = new DirectMessage
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content.Trim(),
                SentAt = UtcNow
            };
            await dbContext.DirectMessages.AddAsync(message);
            await dbContext.SaveChangesAsync();
            return MapToDto(message);
        }

        public async Task<int> MarkConversationReadAsync(string userId, string otherUserId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var unread = await dbContext.DirectMessages
                .Where(message => message.RecipientId == userId
                    && message.SenderId == otherUserId
                    && message.ReadAt == null)
                .ToListAsync();
            var now = UtcNow;
            foreach (var message in unread)
            {
                message.ReadAt = now;
            }
            await dbContext.SaveChangesAsync();
            return unread.Count;
        }

        private static DirectMessageDto MapToDto(DirectMessage message)
        {
            return new DirectMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                RecipientId = message.RecipientId,
                Content = message.Content,
                SentAt = message.SentAt,
                ReadAt = message.ReadAt
            };
        }
    }
}
