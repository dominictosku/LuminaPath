using LuminaPath.Infrastructure.Services.AiChat.Mcp;

namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class ChatToolRegistry
{
    private readonly IReadOnlyDictionary<string, IChatTool> _tools;

    public ChatToolRegistry(IEnumerable<IChatTool> builtIn, McpHostService mcp)
    {
        var map = new Dictionary<string, IChatTool>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in builtIn)
        {
            map[tool.Name] = tool;
        }

        foreach (var handle in mcp.Tools)
        {
            // Namespace MCP tool names with "mcp__<server>__<tool>" so they can't collide with built-ins.
            var exposedName = $"mcp__{Sanitize(handle.Server.Name)}__{Sanitize(handle.Tool.Name)}";
            if (map.ContainsKey(exposedName)) continue;
            map[exposedName] = new McpToolAdapter(mcp, handle, exposedName);
        }

        _tools = map;
    }

    public IReadOnlyCollection<IChatTool> All => _tools.Values.ToList();

    public bool TryGet(string name, out IChatTool tool) => _tools.TryGetValue(name, out tool!);

    private static string Sanitize(string value) =>
        new string(value.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
}
