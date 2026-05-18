namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class GameNewsOptions
{
    public const string SectionName = "GameNews";

    public int CacheTtlHours { get; set; } = 6;
    public int MaxItems { get; set; } = 12;
    public int SteamMaxLength { get; set; } = 600;
    public string SteamFeedNames { get; set; } = "steam_community_announcements";
    public string GoogleNewsBaseUrl { get; set; } = "https://news.google.com/rss/search";
    public string GoogleNewsLocale { get; set; } = "en-US";
    public string GoogleNewsCountry { get; set; } = "US";
}
