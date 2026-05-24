using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LuminaPath.Infrastructure.Services.AiChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LuminaPath.Infrastructure.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ChatService _chat;
    private readonly IServiceProvider _services;

    public ChatController(ChatService chat, IServiceProvider services)
    {
        _chat = chat;
        _services = services;
    }

    [HttpPost("stream")]
    [EnableRateLimiting(RateLimitPolicies.ChatStreaming)]
    [RequestSizeLimit(ChatRequestLimits.MaxRequestBytes)]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";

        var toolContext = new ChatToolContext { UserId = userId, Services = _services };

        await foreach (var ev in _chat.StreamAsync(request, toolContext, cancellationToken))
        {
            await WriteSseAsync(ev, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            if (ev is ChatDoneEvent) break;
        }
    }

    private async Task WriteSseAsync(ChatEvent ev, CancellationToken cancellationToken)
    {
        var (name, payload) = ev switch
        {
            ChatTextEvent t => ("text", JsonSerializer.Serialize(new { text = t.Text }, JsonOpts)),
            ChatToolCallEvent c => ("tool_call", JsonSerializer.Serialize(new { name = c.Name, server = c.Server }, JsonOpts)),
            ChatErrorEvent e => ("error", JsonSerializer.Serialize(new { message = e.Message }, JsonOpts)),
            ChatDoneEvent => ("done", "{}"),
            _ => ("text", "{}"),
        };

        var bytes = Encoding.UTF8.GetBytes($"event: {name}\ndata: {payload}\n\n");
        await Response.Body.WriteAsync(bytes, cancellationToken);
    }
}
