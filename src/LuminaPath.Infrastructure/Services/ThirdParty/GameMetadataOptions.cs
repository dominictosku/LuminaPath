namespace LuminaPath.Infrastructure.Services.ThirdParty;

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

    internal static bool HasValidProvider(GameMetadataOptions options)
    {
        return GameMetadataProviderModes.All.Contains(options.Provider, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool HasValidUrls(GameMetadataOptions options)
    {
        return LuminaPath.Infrastructure.Configuration.InfrastructureOptionValidation.IsHttpUrl(options.TwitchTokenUrl)
            && LuminaPath.Infrastructure.Configuration.InfrastructureOptionValidation.IsHttpUrl(options.IgdbBaseUrl)
            && LuminaPath.Infrastructure.Configuration.InfrastructureOptionValidation.IsHttpUrl(options.RawgBaseUrl);
    }

    internal static bool HasCompleteOptionalCredentials(GameMetadataOptions options)
    {
        var hasClientId = !string.IsNullOrWhiteSpace(options.IgdbClientId);
        var hasClientSecret = !string.IsNullOrWhiteSpace(options.IgdbClientSecret);
        return hasClientId == hasClientSecret;
    }
}

public static class GameMetadataProviderModes
{
    public const string IgdbThenRawg = "IgdbThenRawg";
    public const string RawgThenIgdb = "RawgThenIgdb";
    public const string Igdb = "Igdb";
    public const string Rawg = "Rawg";

    public static readonly string[] All =
    [
        IgdbThenRawg,
        RawgThenIgdb,
        Igdb,
        Rawg
    ];
}
