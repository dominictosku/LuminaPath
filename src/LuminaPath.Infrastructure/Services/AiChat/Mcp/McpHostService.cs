using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LuminaPath.Infrastructure.Services.AiChat.Mcp;

public sealed class McpHostService : IHostedService, IAsyncDisposable
{
    private readonly McpOptions _options;
    private readonly ILogger<McpHostService> _logger;
    private readonly List<ConnectedServer> _connected = new();

    public McpHostService(IOptions<McpOptions> options, ILogger<McpHostService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<McpToolHandle> Tools => _connected
        .SelectMany(c => c.Tools.Select(t => new McpToolHandle(c, t)))
        .ToList();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var config in _options.Servers)
        {
            if (!config.Enabled)
            {
                _logger.LogInformation("MCP server '{Name}' disabled, skipping", config.Name);
                continue;
            }

            if (string.IsNullOrWhiteSpace(config.Command))
            {
                _logger.LogWarning("MCP server '{Name}' has no command, skipping", config.Name);
                continue;
            }

            // Skip when required env vars are placeholders.
            if (config.Env.Values.Any(v => string.IsNullOrWhiteSpace(v)))
            {
                _logger.LogInformation(
                    "MCP server '{Name}' is missing one or more env values; skipping. Set them via env vars or appsettings.",
                    config.Name);
                continue;
            }

            try
            {
                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = config.Name,
                    Command = config.Command,
                    Arguments = config.Args.ToArray(),
                    EnvironmentVariables = config.Env
                        .Where(kv => kv.Value is not null)
                        .ToDictionary(kv => kv.Key, kv => kv.Value!),
                });

                var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

                _connected.Add(new ConnectedServer(config.Name, client, tools));
                _logger.LogInformation("MCP server '{Name}' connected with {Count} tools", config.Name, tools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start MCP server '{Name}'. Chat will work without it.", config.Name);
            }
        }
    }

    public async Task<string> CallToolAsync(
        ConnectedServer server,
        string toolName,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var argDict = JsonElementToDictionary(arguments);
        try
        {
            var result = await server.Client.CallToolAsync(toolName, argDict, cancellationToken: cancellationToken);
            return RenderToolResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP tool '{Tool}' on '{Server}' failed", toolName, server.Name);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        foreach (var server in _connected)
        {
            try
            {
                await server.Client.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }
        _connected.Clear();
    }

    private static Dictionary<string, object?> JsonElementToDictionary(JsonElement args)
    {
        var dict = new Dictionary<string, object?>();
        if (args.ValueKind != JsonValueKind.Object) return dict;
        foreach (var prop in args.EnumerateObject())
        {
            dict[prop.Name] = JsonValueToObject(prop.Value);
        }
        return dict;
    }

    private static object? JsonValueToObject(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.TryGetInt64(out var i) ? i : value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => value.EnumerateArray().Select(JsonValueToObject).ToArray(),
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => JsonValueToObject(p.Value)),
        _ => value.ToString(),
    };

    private static string RenderToolResult(CallToolResult result)
    {
        if (result.Content is null || result.Content.Count == 0)
        {
            return "(empty result)";
        }

        var sb = new System.Text.StringBuilder();
        foreach (var block in result.Content)
        {
            if (block is TextContentBlock text)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(text.Text);
            }
        }

        return sb.Length == 0 ? "(non-text result omitted)" : sb.ToString();
    }
}

public sealed record ConnectedServer(string Name, McpClient Client, IList<McpClientTool> Tools);

public sealed record McpToolHandle(ConnectedServer Server, McpClientTool Tool);
