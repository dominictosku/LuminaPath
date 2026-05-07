namespace LuminaPath.Infrastructure.Services.AiChat;

/// <summary>
/// Streaming chat provider. The internal conversation state uses Anthropic-shaped types
/// (<see cref="AnthropicMessage"/>, <see cref="AnthropicContentBlock"/>) as the canonical format;
/// each provider translates to/from its own wire format.
/// </summary>
public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }

    IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
        IReadOnlyList<AnthropicMessage> messages,
        string? system,
        IReadOnlyList<AnthropicToolDefinition>? tools,
        CancellationToken cancellationToken);
}
