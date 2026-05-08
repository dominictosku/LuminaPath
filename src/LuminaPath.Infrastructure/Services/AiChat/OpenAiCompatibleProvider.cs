using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.AiChat;

/// <summary>
/// Talks to any OpenAI Chat Completions compatible endpoint (the official OpenAI API,
/// Ollama at <c>/v1/chat/completions</c>, LM Studio, vLLM, llama.cpp server, etc.).
/// Translates the canonical Anthropic-shaped messages and tool blocks to/from OpenAI shape
/// and emits the same <see cref="AnthropicStreamEvent"/>s the agent loop already understands.
/// </summary>
public sealed class OpenAiCompatibleProvider : IAiProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiCompatibleProvider> _logger;

    public OpenAiCompatibleProvider(HttpClient http, IOptions<OpenAiOptions> options, ILogger<OpenAiCompatibleProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "openai";

    // Self-hosted endpoints (Ollama) often don't need a key, so configured = base URL set.
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BaseUrl);

    public async IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
        IReadOnlyList<AnthropicMessage> messages,
        string? system,
        IReadOnlyList<AnthropicToolDefinition>? tools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            yield return new StreamErrorEvent("OpenAI-compatible base URL is not configured.");
            yield break;
        }

        var body = BuildRequestBody(messages, system, tools);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Content = JsonContent.Create(body, options: SerializerOptions);

        HttpResponseMessage? response = null;
        string? sendError = null;
        try
        {
            response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to call OpenAI-compatible endpoint at {Url}", _options.BaseUrl);
            sendError = "Could not reach the AI service.";
        }

        if (sendError is not null)
        {
            yield return new StreamErrorEvent(sendError);
            yield break;
        }

        if (response is null || !response.IsSuccessStatusCode)
        {
            var errBody = response is null ? "" : await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OpenAI-compatible endpoint returned {Status}: {Body}", (int?)response?.StatusCode ?? 0, errBody);
            yield return new StreamErrorEvent($"AI service error ({(int?)response?.StatusCode ?? 0}).");
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var toolBuffers = new Dictionary<int, ToolCallBuffer>();
        var startedToolIndices = new HashSet<int>();
        string? finishReason = null;
        var done = false;

        while (!done && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (line.Length == 0) continue;
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var payload = line[5..].Trim();
            if (payload.Length == 0) continue;
            if (payload == "[DONE]") { done = true; break; }

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse OpenAI SSE payload: {Payload}", payload);
                continue;
            }

            foreach (var ev in HandleChunk(doc.RootElement, toolBuffers, startedToolIndices, ref finishReason))
            {
                yield return ev;
            }

            doc.Dispose();
        }

        // Close any open tool blocks so the agent loop can finalize them.
        foreach (var index in startedToolIndices)
        {
            yield return new ContentBlockStopEvent(index);
        }

        var stopReason = finishReason switch
        {
            "tool_calls" => "tool_use",
            "stop" => "end_turn",
            "length" => "max_tokens",
            _ => finishReason,
        };
        yield return new MessageStopEvent(stopReason);
    }

    private static IEnumerable<AnthropicStreamEvent> HandleChunk(
        JsonElement root,
        Dictionary<int, ToolCallBuffer> toolBuffers,
        HashSet<int> startedToolIndices,
        ref string? finishReason)
    {
        var events = new List<AnthropicStreamEvent>();

        if (root.TryGetProperty("error", out var errProp))
        {
            var msg = errProp.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
            events.Add(new StreamErrorEvent(msg ?? "Unknown error"));
            return events;
        }

        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
        {
            return events;
        }

        var choice = choices[0];

        if (choice.TryGetProperty("finish_reason", out var fr) && fr.ValueKind == JsonValueKind.String)
        {
            finishReason = fr.GetString();
        }

        if (!choice.TryGetProperty("delta", out var delta)) return events;

        if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
        {
            var text = content.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                events.Add(new TextDeltaEvent(text));
            }
        }

        if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (var tc in toolCalls.EnumerateArray())
            {
                var index = tc.TryGetProperty("index", out var idx) && idx.ValueKind == JsonValueKind.Number
                    ? idx.GetInt32() : 0;

                if (!toolBuffers.TryGetValue(index, out var buffer))
                {
                    buffer = new ToolCallBuffer();
                    toolBuffers[index] = buffer;
                }

                if (tc.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(idProp.GetString()))
                {
                    buffer.Id = idProp.GetString()!;
                }

                if (tc.TryGetProperty("function", out var fn) && fn.ValueKind == JsonValueKind.Object)
                {
                    if (fn.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(nameProp.GetString()))
                    {
                        buffer.Name = nameProp.GetString()!;
                    }

                    if (fn.TryGetProperty("arguments", out var argsProp) && argsProp.ValueKind == JsonValueKind.String)
                    {
                        var partial = argsProp.GetString() ?? string.Empty;
                        if (partial.Length > 0)
                        {
                            buffer.Arguments.Append(partial);
                            // Emit the start event once we have name + id.
                            if (!startedToolIndices.Contains(index) && !string.IsNullOrEmpty(buffer.Name))
                            {
                                startedToolIndices.Add(index);
                                events.Add(new ToolUseStartEvent(index, buffer.Id, buffer.Name));
                            }
                            if (startedToolIndices.Contains(index))
                            {
                                events.Add(new ToolUseDeltaEvent(index, partial));
                            }
                        }
                    }
                    else if (!startedToolIndices.Contains(index) && !string.IsNullOrEmpty(buffer.Name))
                    {
                        // Some servers emit name/id in one chunk and arguments in following chunks.
                        startedToolIndices.Add(index);
                        events.Add(new ToolUseStartEvent(index, buffer.Id, buffer.Name));
                    }
                }
            }
        }

        return events;
    }

    private object BuildRequestBody(
        IReadOnlyList<AnthropicMessage> messages,
        string? system,
        IReadOnlyList<AnthropicToolDefinition>? tools)
    {
        var translated = TranslateMessages(messages, system);

        var body = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = translated,
            ["max_tokens"] = _options.MaxTokens,
            ["stream"] = true,
        };

        if (tools is { Count: > 0 })
        {
            body["tools"] = tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.InputSchema,
                },
            }).ToArray();

            if (!string.IsNullOrWhiteSpace(_options.ToolChoice))
            {
                body["tool_choice"] = _options.ToolChoice;
            }
        }

        return body;
    }

    private static List<object> TranslateMessages(IReadOnlyList<AnthropicMessage> messages, string? system)
    {
        var result = new List<object>();

        if (!string.IsNullOrWhiteSpace(system))
        {
            result.Add(new { role = "system", content = system });
        }

        foreach (var msg in messages)
        {
            if (msg.Role == "assistant")
            {
                var text = string.Join("\n", msg.Content.OfType<TextBlock>().Select(t => t.Text));
                var toolUses = msg.Content.OfType<ToolUseBlock>().ToList();

                if (toolUses.Count > 0)
                {
                    var toolCalls = toolUses.Select(tu => new
                    {
                        id = tu.Id,
                        type = "function",
                        function = new
                        {
                            name = tu.Name,
                            arguments = JsonSerializer.Serialize(tu.Input, SerializerOptions),
                        },
                    }).ToArray();

                    result.Add(new
                    {
                        role = "assistant",
                        content = string.IsNullOrWhiteSpace(text) ? null : text,
                        tool_calls = toolCalls,
                    });
                }
                else
                {
                    result.Add(new { role = "assistant", content = text });
                }
            }
            else // user (or tool result wrapped as user in Anthropic's format)
            {
                var toolResults = msg.Content.OfType<ToolResultBlock>().ToList();
                foreach (var tr in toolResults)
                {
                    result.Add(new
                    {
                        role = "tool",
                        tool_call_id = tr.ToolUseId,
                        content = tr.Content,
                    });
                }

                var text = string.Join("\n", msg.Content.OfType<TextBlock>().Select(t => t.Text));
                if (!string.IsNullOrWhiteSpace(text) || toolResults.Count == 0)
                {
                    result.Add(new { role = "user", content = text });
                }
            }
        }

        return result;
    }

    private sealed class ToolCallBuffer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public StringBuilder Arguments { get; } = new();
    }
}
