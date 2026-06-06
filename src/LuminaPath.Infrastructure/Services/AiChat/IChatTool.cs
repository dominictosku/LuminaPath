using System.Text.Json;

namespace LuminaPath.Infrastructure.Services.AiChat;

public interface IChatTool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    bool IsWriteAction => false;
    Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken);
}

public sealed class ChatToolContext
{
    public required string UserId { get; init; }
    public required IServiceProvider Services { get; init; }
}
