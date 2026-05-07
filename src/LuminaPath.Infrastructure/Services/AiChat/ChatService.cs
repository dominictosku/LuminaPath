using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class ChatService
{
    private const string SystemPrompt =
        """
        You are LuminaPath's in-app gaming assistant. You help the signed-in user understand their gaming library,
        plan sessions, and answer questions about upcoming game releases. You have tools that query the database
        scoped to the current user, plus optional MCP tools for external information (e.g. web search).

        Guidelines:
        - When the user asks about THEIR data (their library, quests, sessions), use the database tools rather than guessing.
        - For questions about general gaming info or upcoming releases not in the database, prefer MCP tools when available.
        - Today's date is provided in the conversation; use it for relative date questions like "this month".
        - Keep responses concise and skim-friendly. Use short paragraphs or compact lists.
        """;

    private readonly IAiProvider _provider;
    private readonly AiChatOptions _options;
    private readonly ChatToolRegistry _tools;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IAiProvider provider,
        IOptions<AiChatOptions> options,
        ChatToolRegistry tools,
        ILogger<ChatService> logger)
    {
        _provider = provider;
        _options = options.Value;
        _tools = tools;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        ChatToolContext toolContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_provider.IsConfigured)
        {
            yield return new ChatErrorEvent($"AI provider '{_provider.Name}' is not configured.");
            yield return new ChatDoneEvent();
            yield break;
        }

        var messages = BuildInitialMessages(request);
        var toolDefs = _tools.All
            .Select(t => new AnthropicToolDefinition(t.Name, Truncate(t.Description, 1024), t.InputSchema))
            .ToList();

        var system = SystemPrompt + "\n\nToday's date: " + DateTime.UtcNow.ToString("yyyy-MM-dd");

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var assistantBlocks = new List<AnthropicContentBlock>();
            var toolUseBuffers = new Dictionary<int, ToolUseBuffer>();
            string? stopReason = null;
            var hadError = false;

            await foreach (var ev in _provider.StreamAsync(
                messages,
                system,
                toolDefs.Count == 0 ? null : toolDefs,
                cancellationToken))
            {
                switch (ev)
                {
                    case TextDeltaEvent text:
                        yield return new ChatTextEvent(text.Text);
                        AppendText(assistantBlocks, text.Text);
                        break;

                    case ToolUseStartEvent start:
                        toolUseBuffers[start.Index] = new ToolUseBuffer { Id = start.Id, Name = start.Name };
                        break;

                    case ToolUseDeltaEvent delta:
                        if (toolUseBuffers.TryGetValue(delta.Index, out var buf))
                        {
                            buf.JsonBuilder.Append(delta.PartialJson);
                        }
                        break;

                    case ContentBlockStopEvent stop:
                        if (toolUseBuffers.TryGetValue(stop.Index, out var done))
                        {
                            assistantBlocks.Add(BuildToolUseBlock(done));
                        }
                        break;

                    case MessageStopEvent ms:
                        stopReason = ms.StopReason;
                        break;

                    case StreamErrorEvent err:
                        yield return new ChatErrorEvent(err.Message);
                        hadError = true;
                        break;
                }
            }

            if (hadError)
            {
                yield return new ChatDoneEvent();
                yield break;
            }

            if (stopReason != "tool_use")
            {
                yield return new ChatDoneEvent();
                yield break;
            }

            messages = messages.Append(new AnthropicMessage("assistant", assistantBlocks)).ToList();

            var toolResults = new List<AnthropicContentBlock>();
            foreach (var block in assistantBlocks.OfType<ToolUseBlock>())
            {
                if (!_tools.TryGet(block.Name, out var tool))
                {
                    toolResults.Add(new ToolResultBlock(block.Id, $"Tool '{block.Name}' is not available.", IsError: true));
                    continue;
                }

                var server = block.Name.StartsWith("mcp__", StringComparison.Ordinal)
                    ? block.Name.Split("__", 3).ElementAtOrDefault(1) ?? "built-in"
                    : "built-in";
                yield return new ChatToolCallEvent(block.Name, server);

                string output;
                try
                {
                    output = await tool.ExecuteAsync(block.Input, toolContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tool '{Tool}' threw", block.Name);
                    output = JsonSerializer.Serialize(new { error = ex.Message });
                }

                toolResults.Add(new ToolResultBlock(block.Id, output));
            }

            messages = messages.Append(new AnthropicMessage("user", toolResults)).ToList();
        }

        yield return new ChatErrorEvent("Reached max tool iterations without a final answer.");
        yield return new ChatDoneEvent();
    }

    private static List<AnthropicMessage> BuildInitialMessages(ChatRequest request)
    {
        return request.Messages
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new AnthropicMessage(
                m.Role == "assistant" ? "assistant" : "user",
                new List<AnthropicContentBlock> { new TextBlock(m.Content) }))
            .ToList();
    }

    private static void AppendText(List<AnthropicContentBlock> blocks, string text)
    {
        if (blocks.Count > 0 && blocks[^1] is TextBlock last)
        {
            blocks[^1] = new TextBlock(last.Text + text);
        }
        else
        {
            blocks.Add(new TextBlock(text));
        }
    }

    private static ToolUseBlock BuildToolUseBlock(ToolUseBuffer buffer)
    {
        var json = buffer.JsonBuilder.Length == 0 ? "{}" : buffer.JsonBuilder.ToString();
        JsonElement input;
        try
        {
            using var doc = JsonDocument.Parse(json);
            input = doc.RootElement.Clone();
        }
        catch
        {
            using var doc = JsonDocument.Parse("{}");
            input = doc.RootElement.Clone();
        }
        return new ToolUseBlock(buffer.Id, buffer.Name, input);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed class ToolUseBuffer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public StringBuilder JsonBuilder { get; } = new();
    }
}
