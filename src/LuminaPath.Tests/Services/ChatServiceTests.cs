using System.Runtime.CompilerServices;
using System.Text.Json;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.AiChat.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LuminaPath.Tests.Services;

public class ChatServiceTests
{
    [Fact]
    public async Task StreamAsync_WhenProviderNotConfigured_EmitsErrorThenDoneWithoutCallingProvider()
    {
        var provider = new ScriptedProvider(configured: false);
        var service = CreateService(provider);

        var events = await CollectAsync(service);

        var error = Assert.IsType<ChatErrorEvent>(events[0]);
        Assert.Contains("not configured", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<ChatDoneEvent>(events[^1]);
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public async Task StreamAsync_PlainTextAnswer_StreamsTextDeltasThenDone()
    {
        var provider = new ScriptedProvider(
            configured: true,
            new List<AnthropicStreamEvent>
            {
                new TextDeltaEvent("Hello "),
                new TextDeltaEvent("world"),
                new MessageStopEvent("end_turn"),
            });
        var service = CreateService(provider);

        var events = await CollectAsync(service);

        Assert.Collection(
            events,
            e => Assert.Equal("Hello ", Assert.IsType<ChatTextEvent>(e).Text),
            e => Assert.Equal("world", Assert.IsType<ChatTextEvent>(e).Text),
            e => Assert.IsType<ChatDoneEvent>(e));
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task StreamAsync_ToolUse_CallsToolAndThreadsResultBackToProvider()
    {
        var tool = new ScriptedTool("echo", _ => "TOOL_RESULT");
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "echo", "{\"q\":\"hi\"}"),
            TextTurn("final answer"));
        var service = CreateService(provider, tools: tool);

        var events = await CollectAsync(service);

        Assert.Contains(events, e => e is ChatToolCallEvent c && c.Name == "echo" && c.Server == "built-in");
        Assert.Contains(events, e => e is ChatTextEvent t && t.Text == "final answer");
        Assert.IsType<ChatDoneEvent>(events[^1]);

        Assert.Equal(1, tool.CallCount);
        Assert.Equal("hi", tool.LastInput.GetProperty("q").GetString());

        // The second provider call must carry the tool_result back so the
        // model can produce a final answer.
        Assert.Equal(2, provider.Calls.Count);
        var toolResult = SingleToolResult(provider.Calls[1]);
        Assert.Equal("TOOL_RESULT", toolResult.Content);
        Assert.False(toolResult.IsError);
    }

    [Fact]
    public async Task StreamAsync_UnknownTool_AddsErrorResultWithoutEmittingToolCall()
    {
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "missing_tool", "{}"),
            TextTurn("ok"));
        var service = CreateService(provider); // no tools registered

        var events = await CollectAsync(service);

        Assert.DoesNotContain(events, e => e is ChatToolCallEvent);
        Assert.Contains(events, e => e is ChatTextEvent t && t.Text == "ok");

        var toolResult = SingleToolResult(provider.Calls[1]);
        Assert.True(toolResult.IsError);
        Assert.Contains("missing_tool", toolResult.Content);
        Assert.Contains("not available", toolResult.Content);
    }

    [Fact]
    public async Task StreamAsync_WriteToolsDisabled_AddsDisabledResultWithoutCallingTool()
    {
        var tool = new ScriptedTool("create_quest", _ => "created", isWriteAction: true);
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "create_quest", "{}"),
            TextTurn("ok"));
        var service = CreateService(provider, enableWriteTools: false, tools: tool);

        var events = await CollectAsync(service);

        Assert.DoesNotContain(events, e => e is ChatToolCallEvent);
        Assert.Contains(events, e => e is ChatTextEvent t && t.Text == "ok");
        Assert.Equal(0, tool.CallCount);

        var toolResult = SingleToolResult(provider.Calls[1]);
        Assert.True(toolResult.IsError);
        Assert.Contains("disabled", toolResult.Content);
    }

    [Fact]
    public async Task StreamAsync_McpToolsDisabled_AddsDisabledResultWithoutCallingTool()
    {
        var tool = new ScriptedTool("mcp__brave__search", _ => "result");
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "mcp__brave__search", "{}"),
            TextTurn("ok"));
        var service = CreateService(provider, enableMcpTools: false, tools: tool);

        var events = await CollectAsync(service);

        Assert.DoesNotContain(events, e => e is ChatToolCallEvent);
        Assert.Contains(events, e => e is ChatTextEvent t && t.Text == "ok");
        Assert.Equal(0, tool.CallCount);

        var toolResult = SingleToolResult(provider.Calls[1]);
        Assert.True(toolResult.IsError);
        Assert.Contains("disabled", toolResult.Content);
    }

    [Fact]
    public async Task StreamAsync_ToolThrows_SerializesErrorAsResultAndContinues()
    {
        var tool = new ScriptedTool("boom", _ => throw new InvalidOperationException("kaboom"));
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "boom", "{}"),
            TextTurn("recovered"));
        var service = CreateService(provider, tools: tool);

        var events = await CollectAsync(service);

        // A found tool still emits its call event before failing.
        Assert.Contains(events, e => e is ChatToolCallEvent c && c.Name == "boom");
        Assert.Contains(events, e => e is ChatTextEvent t && t.Text == "recovered");
        Assert.IsType<ChatDoneEvent>(events[^1]);

        var toolResult = SingleToolResult(provider.Calls[1]);
        Assert.Contains("kaboom", toolResult.Content);
        Assert.False(toolResult.IsError);
    }

    [Fact]
    public async Task StreamAsync_MalformedToolInputJson_FallsBackToEmptyObject()
    {
        var tool = new ScriptedTool("echo", _ => "ok");
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "echo", "{this is not valid json"),
            TextTurn("done"));
        var service = CreateService(provider, tools: tool);

        await CollectAsync(service);

        Assert.Equal(JsonValueKind.Object, tool.LastInput.ValueKind);
        Assert.Empty(tool.LastInput.EnumerateObject().ToList());
    }

    [Fact]
    public async Task StreamAsync_McpToolName_ParsesServerSegmentForToolCallEvent()
    {
        var tool = new ScriptedTool("mcp__brave__search", _ => "result");
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("call-1", "mcp__brave__search", "{}"),
            TextTurn("done"));
        var service = CreateService(provider, tools: tool);

        var events = await CollectAsync(service);

        var toolCall = Assert.Single(events.OfType<ChatToolCallEvent>());
        Assert.Equal("mcp__brave__search", toolCall.Name);
        Assert.Equal("brave", toolCall.Server);
    }

    [Fact]
    public async Task StreamAsync_StopsAtMaxToolIterations_EmitsLimitError()
    {
        var tool = new ScriptedTool("loop", _ => "again");
        // The model never finalizes: every turn is another tool_use.
        var provider = new ScriptedProvider(
            configured: true,
            ToolTurn("c1", "loop", "{}"),
            ToolTurn("c2", "loop", "{}"));
        var service = CreateService(provider, maxIterations: 2, tools: tool);

        var events = await CollectAsync(service);

        var error = Assert.Single(events.OfType<ChatErrorEvent>());
        Assert.Contains("max tool iterations", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<ChatDoneEvent>(events[^1]);
        Assert.Equal(2, provider.Calls.Count);
        Assert.Equal(2, tool.CallCount);
    }

    [Fact]
    public async Task StreamAsync_ProviderStreamError_SurfacesErrorThenDoneWithoutAnotherTurn()
    {
        var provider = new ScriptedProvider(
            configured: true,
            new List<AnthropicStreamEvent>
            {
                new TextDeltaEvent("partial"),
                new StreamErrorEvent("upstream 500"),
            });
        var service = CreateService(provider);

        var events = await CollectAsync(service);

        Assert.Contains(events, e => e is ChatTextEvent t && t.Text == "partial");
        var error = Assert.Single(events.OfType<ChatErrorEvent>());
        Assert.Equal("upstream 500", error.Message);
        Assert.IsType<ChatDoneEvent>(events[^1]);
        Assert.Single(provider.Calls);
    }

    // ---------- helpers ----------

    private static ChatService CreateService(
        IAiProvider provider,
        int maxIterations = 8,
        bool enableMcpTools = true,
        bool enableWriteTools = true,
        params IChatTool[] tools)
    {
        var mcp = new McpHostService(
            Options.Create(new McpOptions()),
            new Mock<ILogger<McpHostService>>().Object);
        var registry = new ChatToolRegistry(tools, mcp);
        var resolver = CreateSettingsResolver(maxIterations, enableMcpTools, enableWriteTools);
        return new ChatService(provider, resolver, registry, new Mock<ILogger<ChatService>>().Object);
    }

    private static AiChatRuntimeSettingsResolver CreateSettingsResolver(
        int maxIterations,
        bool enableMcpTools,
        bool enableWriteTools)
    {
        var dbOptions = new DbContextOptionsBuilder<LuminaPathDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using (var context = new LuminaPathDbContext(dbOptions))
        {
            context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = ApplicationSettingsService.AiChatMaxToolIterations,
                Value = maxIterations.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
            context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = ApplicationSettingsService.AiChatEnableMcpTools,
                Value = enableMcpTools.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
            context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = ApplicationSettingsService.AiChatEnableWriteTools,
                Value = enableWriteTools.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        var settings = new ApplicationSettingsService(new TestDbContextFactory(dbOptions));
        return new AiChatRuntimeSettingsResolver(
            Options.Create(new AiChatOptions { MaxToolIterations = maxIterations }),
            Options.Create(new AnthropicOptions()),
            Options.Create(new OpenAiOptions()),
            settings);
    }

    private static async Task<List<ChatEvent>> CollectAsync(ChatService service)
    {
        var request = new ChatRequest
        {
            Messages = new List<ChatRequestMessage>
            {
                new() { Role = "user", Content = "hi" },
            },
        };
        var context = new ChatToolContext
        {
            UserId = "user-1",
            Services = new Mock<IServiceProvider>().Object,
        };

        var events = new List<ChatEvent>();
        await foreach (var ev in service.StreamAsync(request, context, CancellationToken.None))
        {
            events.Add(ev);
        }

        return events;
    }

    private static ToolResultBlock SingleToolResult(IReadOnlyList<AnthropicMessage> messages)
        => Assert.Single(messages.SelectMany(m => m.Content).OfType<ToolResultBlock>());

    private static IReadOnlyList<AnthropicStreamEvent> ToolTurn(string id, string name, string inputJson)
        => new List<AnthropicStreamEvent>
        {
            new ToolUseStartEvent(0, id, name),
            new ToolUseDeltaEvent(0, inputJson),
            new ContentBlockStopEvent(0),
            new MessageStopEvent("tool_use"),
        };

    private static IReadOnlyList<AnthropicStreamEvent> TextTurn(string text)
        => new List<AnthropicStreamEvent>
        {
            new TextDeltaEvent(text),
            new MessageStopEvent("end_turn"),
        };

    private sealed class ScriptedProvider : IAiProvider
    {
        private readonly Queue<IReadOnlyList<AnthropicStreamEvent>> _turns;

        public ScriptedProvider(bool configured, params IReadOnlyList<AnthropicStreamEvent>[] turns)
        {
            IsConfigured = configured;
            _turns = new Queue<IReadOnlyList<AnthropicStreamEvent>>(turns);
        }

        public string Name => "scripted";

        public bool IsConfigured { get; }

        // One entry per StreamAsync invocation, capturing the conversation
        // snapshot the service passed in (so tests can assert tool threading).
        public List<IReadOnlyList<AnthropicMessage>> Calls { get; } = new();

        public async IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
            IReadOnlyList<AnthropicMessage> messages,
            string? system,
            IReadOnlyList<AnthropicToolDefinition>? tools,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Calls.Add(messages);
            var turn = _turns.Count > 0 ? _turns.Dequeue() : new List<AnthropicStreamEvent>();
            foreach (var ev in turn)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return ev;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class ScriptedTool : IChatTool
    {
        private static readonly JsonElement ObjectSchema = ParseObjectSchema();
        private readonly Func<JsonElement, string> _handler;

        public ScriptedTool(string name, Func<JsonElement, string> handler, bool isWriteAction = false)
        {
            Name = name;
            _handler = handler;
            IsWriteAction = isWriteAction;
        }

        public string Name { get; }

        public string Description => "scripted test tool";

        public JsonElement InputSchema => ObjectSchema;

        public bool IsWriteAction { get; }

        public int CallCount { get; private set; }

        public JsonElement LastInput { get; private set; }

        public Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            LastInput = arguments;
            return Task.FromResult(_handler(arguments));
        }

        private static JsonElement ParseObjectSchema()
        {
            using var doc = JsonDocument.Parse("{\"type\":\"object\"}");
            return doc.RootElement.Clone();
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<LuminaPathDbContext>
    {
        private readonly DbContextOptions<LuminaPathDbContext> _options;

        public TestDbContextFactory(DbContextOptions<LuminaPathDbContext> options)
        {
            _options = options;
        }

        public LuminaPathDbContext CreateDbContext()
        {
            return new LuminaPathDbContext(_options);
        }

        public ValueTask<LuminaPathDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<LuminaPathDbContext>(CreateDbContext());
        }
    }
}
