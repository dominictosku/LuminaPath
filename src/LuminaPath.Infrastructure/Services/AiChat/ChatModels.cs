using System.Text.Json.Serialization;

namespace LuminaPath.Infrastructure.Services.AiChat;

public static class ChatRequestLimits
{
    public const int MaxRequestBytes = 32 * 1024;
    public const int MaxMessages = 20;
    public const int MaxMessageCharacters = 4_000;
    public const int MaxTotalCharacters = 12_000;
}

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
