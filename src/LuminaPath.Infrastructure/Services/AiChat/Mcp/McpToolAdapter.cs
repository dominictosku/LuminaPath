using System.Text.Json;

namespace LuminaPath.Infrastructure.Services.AiChat.Mcp;

public sealed class McpToolAdapter : IChatTool
{
    private readonly McpHostService _host;
    private readonly McpToolHandle _handle;

    public McpToolAdapter(McpHostService host, McpToolHandle handle, string exposedName)
    {
        _host = host;
        _handle = handle;
        Name = exposedName;
        Description = string.IsNullOrWhiteSpace(handle.Tool.Description)
            ? $"MCP tool '{handle.Tool.Name}' from server '{handle.Server.Name}'"
            : handle.Tool.Description!;

        InputSchema = handle.Tool.JsonSchema;
    }

    public string Name { get; }
    public string Description { get; }
    public JsonElement InputSchema { get; }

    public Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
        => _host.CallToolAsync(_handle.Server, _handle.Tool.Name, arguments, cancellationToken);
}
