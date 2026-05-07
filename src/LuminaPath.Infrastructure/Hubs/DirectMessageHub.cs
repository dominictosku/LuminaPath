using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Hubs
{
    [Authorize]
    public class DirectMessageHub : Hub
    {
        private readonly DirectMessageService _messageService;

        public DirectMessageHub(DirectMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task<DirectMessageDto?> Send(string recipientId, string content)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new HubException("Not authenticated.");
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
