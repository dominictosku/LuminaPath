using System.Runtime.CompilerServices;

namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class RuntimeAiProvider : IAiProvider
{
    private readonly AiChatRuntimeSettingsResolver _settingsResolver;
    private readonly AnthropicClient _anthropic;
    private readonly OpenAiCompatibleProvider _openAi;

    public RuntimeAiProvider(
        AiChatRuntimeSettingsResolver settingsResolver,
        AnthropicClient anthropic,
        OpenAiCompatibleProvider openAi)
    {
        _settingsResolver = settingsResolver;
        _anthropic = anthropic;
        _openAi = openAi;
    }

    public string Name => "runtime";

    public bool IsConfigured => true;

    public async IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
        IReadOnlyList<AnthropicMessage> messages,
        string? system,
        IReadOnlyList<AnthropicToolDefinition>? tools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var settings = await _settingsResolver.GetAsync(cancellationToken);
        if (!settings.Enabled)
        {
            yield return new StreamErrorEvent("AI chat is disabled by an administrator.");
            yield break;
        }

        var provider = NormalizeProvider(settings.Provider);
        if (provider is "openai" or "ollama")
        {
            await foreach (var ev in _openAi.StreamAsync(messages, system, tools, settings.OpenAi, cancellationToken))
            {
                yield return ev;
            }

            yield break;
        }

        await foreach (var ev in _anthropic.StreamAsync(messages, system, tools, settings.Anthropic, cancellationToken))
        {
            yield return ev;
        }
    }

    private static string NormalizeProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "anthropic"
            : provider.Trim().ToLowerInvariant();
    }
}
