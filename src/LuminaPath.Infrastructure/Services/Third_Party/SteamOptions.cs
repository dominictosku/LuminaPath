namespace LuminaPath.Infrastructure.Services.Third_Party;

public sealed class SteamOptions
{
    public const string SectionName = "Steam";

    /// <summary>
    /// Server-side Steam Web API key (https://steamcommunity.com/dev/apikey).
    /// </summary>
    public string? ApiKey { get; set; }

    public string ApiBaseUrl { get; set; } = "https://api.steampowered.com";

    public string StoreBaseUrl { get; set; } = "https://store.steampowered.com";
}
