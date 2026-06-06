using LuminaPath.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class AiChatRuntimeSettingsResolver
{
    private readonly AiChatOptions _chatOptions;
    private readonly AnthropicOptions _anthropicOptions;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ApplicationSettingsService _settings;
    private AiChatRuntimeSettings? _cachedSettings;

    public AiChatRuntimeSettingsResolver(
        IOptions<AiChatOptions> chatOptions,
        IOptions<AnthropicOptions> anthropicOptions,
        IOptions<OpenAiOptions> openAiOptions,
        ApplicationSettingsService settings)
    {
        _chatOptions = chatOptions.Value;
        _anthropicOptions = anthropicOptions.Value;
        _openAiOptions = openAiOptions.Value;
        _settings = settings;
    }

    public async Task<AiChatRuntimeSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedSettings is not null)
        {
            return _cachedSettings;
        }

        var stored = await _settings.GetAiChatSettingsAsync(cancellationToken);

        _cachedSettings = new AiChatRuntimeSettings(
            Enabled: ParseBool(stored.Enabled, fallback: true),
            Provider: NormalizeProvider(FirstNonEmpty(stored.Provider, _chatOptions.Provider, "anthropic")),
            MaxToolIterations: ParseInt(stored.MaxToolIterations, _chatOptions.MaxToolIterations, 1, 32),
            EnableMcpTools: ParseBool(stored.EnableMcpTools, fallback: true),
            EnableWriteTools: ParseBool(stored.EnableWriteTools, fallback: true),
            Anthropic: new AnthropicRuntimeSettings(
                ApiKey: FirstNonEmpty(stored.AnthropicApiKey, _anthropicOptions.ApiKey),
                Model: FirstNonEmpty(stored.AnthropicModel, _anthropicOptions.Model, "claude-sonnet-4-6"),
                MaxTokens: ParseInt(stored.AnthropicMaxTokens, _anthropicOptions.MaxTokens, 256, 200_000),
                BaseUrl: FirstNonEmpty(stored.AnthropicBaseUrl, _anthropicOptions.BaseUrl, "https://api.anthropic.com"),
                AnthropicVersion: FirstNonEmpty(stored.AnthropicVersion, _anthropicOptions.AnthropicVersion, "2023-06-01")),
            OpenAi: new OpenAiRuntimeSettings(
                ApiKey: FirstNonEmpty(stored.OpenAiApiKey, _openAiOptions.ApiKey),
                BaseUrl: FirstNonEmpty(stored.OpenAiBaseUrl, _openAiOptions.BaseUrl, "http://localhost:11434/v1"),
                Model: FirstNonEmpty(stored.OpenAiModel, _openAiOptions.Model, "llama3.2"),
                ToolChoice: FirstNonEmpty(stored.OpenAiToolChoice, _openAiOptions.ToolChoice),
                MaxTokens: ParseInt(stored.OpenAiMaxTokens, _openAiOptions.MaxTokens, 256, 200_000)));
        return _cachedSettings;
    }

    public void ClearCache()
    {
        _cachedSettings = null;
    }

    private static bool ParseBool(string value, bool fallback)
    {
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static int ParseInt(string value, int fallback, int min, int max)
    {
        return int.TryParse(value, out var parsed)
            ? Math.Clamp(parsed, min, max)
            : Math.Clamp(fallback, min, max);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string NormalizeProvider(string provider)
    {
        return provider.Trim().ToLowerInvariant() switch
        {
            "openai" => "openai",
            "ollama" => "ollama",
            _ => "anthropic"
        };
    }
}

public sealed record AiChatRuntimeSettings(
    bool Enabled,
    string Provider,
    int MaxToolIterations,
    bool EnableMcpTools,
    bool EnableWriteTools,
    AnthropicRuntimeSettings Anthropic,
    OpenAiRuntimeSettings OpenAi);

public sealed record AnthropicRuntimeSettings(
    string ApiKey,
    string Model,
    int MaxTokens,
    string BaseUrl,
    string AnthropicVersion);

public sealed record OpenAiRuntimeSettings(
    string ApiKey,
    string BaseUrl,
    string Model,
    string ToolChoice,
    int MaxTokens);
