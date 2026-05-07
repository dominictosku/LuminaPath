using System.Text.Json;
using System.Text.Json.Serialization;

namespace LuminaPath.Infrastructure.Services.AiChat;

// ---------- Outgoing request ----------

public sealed record AnthropicMessageRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("messages")] IReadOnlyList<AnthropicMessage> Messages,
    [property: JsonPropertyName("system"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? System,
    [property: JsonPropertyName("tools"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<AnthropicToolDefinition>? Tools,
    [property: JsonPropertyName("stream")] bool Stream
);

public sealed record AnthropicMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] IReadOnlyList<AnthropicContentBlock> Content
);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextBlock), "text")]
[JsonDerivedType(typeof(ToolUseBlock), "tool_use")]
[JsonDerivedType(typeof(ToolResultBlock), "tool_result")]
public abstract record AnthropicContentBlock;

public sealed record TextBlock([property: JsonPropertyName("text")] string Text) : AnthropicContentBlock;

public sealed record ToolUseBlock(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("input")] JsonElement Input
) : AnthropicContentBlock;

public sealed record ToolResultBlock(
    [property: JsonPropertyName("tool_use_id")] string ToolUseId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("is_error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool IsError = false
) : AnthropicContentBlock;

public sealed record AnthropicToolDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("input_schema")] JsonElement InputSchema
);

// ---------- Streaming events ----------

public abstract record AnthropicStreamEvent;

public sealed record TextDeltaEvent(string Text) : AnthropicStreamEvent;
public sealed record ToolUseStartEvent(int Index, string Id, string Name) : AnthropicStreamEvent;
public sealed record ToolUseDeltaEvent(int Index, string PartialJson) : AnthropicStreamEvent;
public sealed record ContentBlockStopEvent(int Index) : AnthropicStreamEvent;
public sealed record MessageStopEvent(string? StopReason) : AnthropicStreamEvent;
public sealed record StreamErrorEvent(string Message) : AnthropicStreamEvent;
