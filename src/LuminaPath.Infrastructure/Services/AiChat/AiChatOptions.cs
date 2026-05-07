namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class AiChatOptions
{
    public const string SectionName = "AiChat";

    /// <summary>
    /// Which provider to route requests through. Supported values: "anthropic", "openai".
    /// "openai" covers any OpenAI-compatible endpoint, including Ollama and LM Studio.
    /// </summary>
    public string Provider { get; set; } = "anthropic";

    /// <summary>
    /// Maximum number of tool-use round-trips before forcing the agent to give a final answer.
    /// </summary>
    public int MaxToolIterations { get; set; } = 8;
}

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    /// <summary>
    /// API key. Optional for self-hosted endpoints like Ollama.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL up to (and including) the OpenAI-compatible API root, e.g.
    /// "https://api.openai.com/v1" or "http://localhost:11434/v1" for Ollama.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";

    /// <summary>
    /// Model identifier to send. e.g. "gpt-4o-mini", "llama3.2", "qwen2.5:14b".
    /// </summary>
    public string Model { get; set; } = "llama3.2";

    public int MaxTokens { get; set; } = 4096;
}
