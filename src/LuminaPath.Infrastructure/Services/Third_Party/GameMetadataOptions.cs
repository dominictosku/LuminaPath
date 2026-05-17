namespace LuminaPath.Infrastructure.Services.Third_Party;

public sealed class GameMetadataOptions
{
    public const string SectionName = "GameMetadata";

    public string Provider { get; set; } = GameMetadataProviderModes.IgdbThenRawg;
    public string IgdbClientId { get; set; } = string.Empty;
    public string IgdbClientSecret { get; set; } = string.Empty;
    public string TwitchTokenUrl { get; set; } = "https://id.twitch.tv/oauth2/token";
    public string IgdbBaseUrl { get; set; } = "https://api.igdb.com/v4";
    public string RawgApiKey { get; set; } = string.Empty;
    public string RawgBaseUrl { get; set; } = "https://api.rawg.io/api";
}

public static class GameMetadataProviderModes
{
    public const string IgdbThenRawg = "IgdbThenRawg";
    public const string RawgThenIgdb = "RawgThenIgdb";
    public const string Igdb = "Igdb";
    public const string Rawg = "Rawg";
}
