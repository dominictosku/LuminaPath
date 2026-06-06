using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class AnthropicClient : IAiProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicClient> _logger;

    public AnthropicClient(HttpClient http, IOptions<AnthropicOptions> options, ILogger<AnthropicClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "anthropic";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
        IReadOnlyList<AnthropicMessage> messages,
        string? system,
        IReadOnlyList<AnthropicToolDefinition>? tools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new AnthropicRuntimeSettings(
            _options.ApiKey ?? string.Empty,
            _options.Model,
            _options.MaxTokens,
            _options.BaseUrl,
            _options.AnthropicVersion);

        await foreach (var ev in StreamAsync(messages, system, tools, options, cancellationToken))
        {
            yield return ev;
        }
    }

    public async IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
        IReadOnlyList<AnthropicMessage> messages,
        string? system,
        IReadOnlyList<AnthropicToolDefinition>? tools,
        AnthropicRuntimeSettings options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            yield return new StreamErrorEvent("Anthropic API key is not configured.");
            yield break;
        }

        var request = new AnthropicMessageRequest(
            Model: options.Model,
            MaxTokens: options.MaxTokens,
            Messages: messages,
            System: system,
            Tools: tools is { Count: > 0 } ? tools : null,
            Stream: true);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/v1/messages");
        httpRequest.Headers.Add("x-api-key", options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", options.AnthropicVersion);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Content = JsonContent.Create(request, options: SerializerOptions);

        HttpResponseMessage? response = null;
        string? sendError = null;
        try
        {
            response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to call Anthropic API");
            sendError = "Could not reach the AI service.";
        }

        if (sendError is not null)
        {
            yield return new StreamErrorEvent(sendError);
            yield break;
        }

        if (response is null || !response.IsSuccessStatusCode)
        {
            var body = response is null ? "" : await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Anthropic API returned {Status}: {Body}", (int?)response?.StatusCode ?? 0, body);
            yield return new StreamErrorEvent($"AI service error ({(int?)response?.StatusCode ?? 0}).");
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;

            if (line.Length == 0)
            {
                currentEvent = null;
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                currentEvent = line[6..].Trim();
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var payload = line[5..].Trim();
            if (payload.Length == 0) continue;

            AnthropicStreamEvent? parsed;
            try
            {
                parsed = ParseEvent(currentEvent, payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse SSE payload: {Payload}", payload);
                continue;
            }

            if (parsed is not null)
            {
                yield return parsed;
            }
        }
    }

    private static AnthropicStreamEvent? ParseEvent(string? eventType, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var type = eventType ?? (root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null);

        switch (type)
        {
            case "content_block_start":
                if (root.TryGetProperty("content_block", out var startBlock))
                {
                    var blockType = startBlock.GetProperty("type").GetString();
                    var index = root.GetProperty("index").GetInt32();
                    if (blockType == "tool_use")
                    {
                        return new ToolUseStartEvent(
                            index,
                            startBlock.GetProperty("id").GetString() ?? string.Empty,
                            startBlock.GetProperty("name").GetString() ?? string.Empty);
                    }
                }
                return null;

            case "content_block_delta":
                {
                    var delta = root.GetProperty("delta");
                    var deltaType = delta.GetProperty("type").GetString();
                    var index = root.GetProperty("index").GetInt32();
                    return deltaType switch
                    {
                        "text_delta" => new TextDeltaEvent(delta.GetProperty("text").GetString() ?? string.Empty),
                        "input_json_delta" => new ToolUseDeltaEvent(index, delta.GetProperty("partial_json").GetString() ?? string.Empty),
                        _ => null,
                    };
                }

            case "content_block_stop":
                return new ContentBlockStopEvent(root.GetProperty("index").GetInt32());

            case "message_delta":
                if (root.TryGetProperty("delta", out var msgDelta) && msgDelta.TryGetProperty("stop_reason", out var stopReason))
                {
                    return new MessageStopEvent(stopReason.GetString());
                }
                return null;

            case "error":
                {
                    var message = root.TryGetProperty("error", out var errProp) && errProp.TryGetProperty("message", out var msgProp)
                        ? msgProp.GetString() ?? "Unknown error"
                        : "Unknown error";
                    return new StreamErrorEvent(message);
                }

            default:
                return null;
        }
    }
}
