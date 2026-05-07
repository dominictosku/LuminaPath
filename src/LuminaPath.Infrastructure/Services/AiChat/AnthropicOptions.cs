namespace LuminaPath.Infrastructure.Services.AiChat;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-4-6";
    public int MaxTokens { get; set; } = 4096;
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string AnthropicVersion { get; set; } = "2023-06-01";
}
