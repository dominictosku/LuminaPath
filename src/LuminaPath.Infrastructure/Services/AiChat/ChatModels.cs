using System.Text.Json.Serialization;

namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class ChatRequest
{
    [JsonPropertyName("messages")]
    public List<ChatRequestMessage> Messages { get; set; } = new();
}

public sealed class ChatRequestMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

// Events streamed back to the frontend (one per SSE `event:` block).
public abstract record ChatEvent;

public sealed record ChatTextEvent(string Text) : ChatEvent;
public sealed record ChatToolCallEvent(string Name, string Server) : ChatEvent;
public sealed record ChatErrorEvent(string Message) : ChatEvent;
public sealed record ChatDoneEvent : ChatEvent;
