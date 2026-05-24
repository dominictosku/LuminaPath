using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Hubs
{
    [Authorize]
    public class DirectMessageHub : Hub
    {
        private readonly DirectMessageService _messageService;
        private readonly UserActionRateLimiter _rateLimiter;

        public DirectMessageHub(DirectMessageService messageService, UserActionRateLimiter rateLimiter)
        {
            _messageService = messageService;
            _rateLimiter = rateLimiter;
        }

        public async Task<DirectMessageDto?> Send(string recipientId, string content)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new HubException("Not authenticated.");
            }

            if (!await _rateLimiter.TryAcquireDirectMessageAsync(senderId, Context.ConnectionAborted))
            {
                throw new HubException("Too many messages. Please wait and try again.");
            }

            var result = await _messageService.SendAsync(senderId, recipientId, content);
            if (result.IsError)
            {
                var error = result.Match(_ => string.Empty, failure => string.Join("; ", failure.errorMessage));
                throw new HubException(error);
            }
            var sent = result.Match(value => value, _ => null!);
            await Clients.User(recipientId).SendAsync("MessageReceived", sent);
            await Clients.User(senderId).SendAsync("MessageSent", sent);
            return sent;
        }

        public async Task MarkRead(string otherUserId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }
            var count = await _messageService.MarkConversationReadAsync(userId, otherUserId);
            if (count > 0)
            {
                await Clients.User(otherUserId).SendAsync("MessagesRead", new { byUserId = userId });
            }
        }
    }

    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
