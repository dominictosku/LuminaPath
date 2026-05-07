namespace LuminaPath.Infrastructure.Services.AiChat.Mcp;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public List<McpServerConfig> Servers { get; set; } = new();
}

public sealed class McpServerConfig
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string?> Env { get; set; } = new();
}
