using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class GameNewsService
{
    private const string CacheVersion = "v1";
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IDistributedCache _cache;
    private readonly ApplicationSettingsService _settings;
    private readonly GameNewsOptions _options;
    private readonly ILogger<GameNewsService> _logger;

    public GameNewsService(
        HttpClient http,
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IDistributedCache cache,
        ApplicationSettingsService settings,
        IOptions<GameNewsOptions> options,
        ILogger<GameNewsService> logger)
    {
        _http = http;
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _settings = settings;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameNewsItemDto>?> GetNewsAsync(int gameId, bool refresh, CancellationToken cancellationToken)
    {
        if (!await _settings.GetNewsEnabledAsync(cancellationToken))
        {
            return Array.Empty<GameNewsItemDto>();
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var game = await context.Games
            .AsNoTracking()
            .Include(g => g.ExternalIds)
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.ExternalIds
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (game is null)
        {
            return null;
        }

        var customRssUrl = await _settings.GetNewsCustomRssUrlAsync(cancellationToken);
        var cacheKey = $"game-news:{CacheVersion}:{game.Id}:{CreateSettingsHash(customRssUrl)}";
        if (!refresh)
        {
            var cached = await TryGetCachedAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var steamAppId = game.ExternalIds.GetExternalId(ExternalMediaProvider.Steam);
        var items = await FetchSteamNewsAsync(steamAppId, cancellationToken);
        if (items.Count == 0)
        {
            items = !string.IsNullOrWhiteSpace(customRssUrl)
                ? await FetchRssNewsAsync(BuildCustomRssUrl(customRssUrl, game.Name), "CustomRss", "Custom RSS", cancellationToken)
                : await FetchGoogleNewsAsync(game.Name, cancellationToken);
        }

        items = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(item => item.PublishedAt ?? DateTime.MinValue)
            .Take(Math.Max(_options.MaxItems, 1))
            .ToList();

        await TrySetCachedAsync(cacheKey, items, cancellationToken);
        return items;
    }

    private async Task<List<GameNewsItemDto>> FetchSteamNewsAsync(string? steamAppId, CancellationToken cancellationToken)
    {
        if (!uint.TryParse(steamAppId, out var appId) || appId == 0)
        {
            return new List<GameNewsItemDto>();
        }

        var url = "https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/"
            + $"?appid={appId}"
            + $"&count={Math.Max(_options.MaxItems, 1)}"
            + $"&maxlength={Math.Max(_options.SteamMaxLength, 120)}"
            + "&format=json";

        if (!string.IsNullOrWhiteSpace(_options.SteamFeedNames))
        {
            url += $"&feeds={Uri.EscapeDataString(_options.SteamFeedNames)}";
        }

        try
        {
            var response = await _http.GetFromJsonAsync<SteamNewsResponse>(url, JsonOptions, cancellationToken);
            return response?.AppNews.NewsItems?
                .Select(item => new GameNewsItemDto
                {
                    Title = CleanText(item.Title),
                    Summary = CleanText(item.Contents),
                    Url = item.Url ?? string.Empty,
                    Source = string.IsNullOrWhiteSpace(item.FeedLabel) ? "Steam" : item.FeedLabel,
                    Provider = "Steam",
                    PublishedAt = item.Date > 0 ? UnixEpoch.AddSeconds(item.Date) : null,
                })
                .ToList() ?? new List<GameNewsItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam news for app id {SteamAppId}", steamAppId);
            return new List<GameNewsItemDto>();
        }
    }

    private async Task<List<GameNewsItemDto>> FetchGoogleNewsAsync(string gameName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            return new List<GameNewsItemDto>();
        }

        var locale = string.IsNullOrWhiteSpace(_options.GoogleNewsLocale) ? "en-US" : _options.GoogleNewsLocale;
        var country = string.IsNullOrWhiteSpace(_options.GoogleNewsCountry) ? "US" : _options.GoogleNewsCountry;
        var query = Uri.EscapeDataString($"\"{gameName}\" video game");
        var url = $"{_options.GoogleNewsBaseUrl.TrimEnd('/')}?q={query}&hl={locale}&gl={country}&ceid={country}:en";

        return await FetchRssNewsAsync(url, "GoogleNews", "Google News", cancellationToken);
    }

    private async Task<List<GameNewsItemDto>> FetchRssNewsAsync(
        string url,
        string provider,
        string defaultSource,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await _http.GetStreamAsync(url, cancellationToken);
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

            return document.Descendants("item")
                .Select(item =>
                {
                    var source = item.Elements().FirstOrDefault(element => element.Name.LocalName == "source");
                    var sourceName = CleanText(source?.Value);
                    return new GameNewsItemDto
                    {
                        Title = CleanText(item.Element("title")?.Value),
                        Summary = CleanText(item.Element("description")?.Value),
                        Url = item.Element("link")?.Value ?? string.Empty,
                        Source = string.IsNullOrWhiteSpace(sourceName) ? defaultSource : sourceName,
                        Provider = provider,
                        PublishedAt = ParseRssDate(item.Element("pubDate")?.Value),
                    };
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch RSS news from {RssUrl}", url);
            return new List<GameNewsItemDto>();
        }
    }

    private static string BuildCustomRssUrl(string template, string gameName)
    {
        var query = Uri.EscapeDataString($"\"{gameName}\" video game");
        var encodedGame = Uri.EscapeDataString(gameName);
        return template
            .Replace("{query}", query, StringComparison.OrdinalIgnoreCase)
            .Replace("{game}", encodedGame, StringComparison.OrdinalIgnoreCase)
            .Replace("{GameName}", encodedGame, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateSettingsHash(string customRssUrl)
    {
        var value = string.IsNullOrWhiteSpace(customRssUrl) ? "default" : customRssUrl.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }

    private async Task<IReadOnlyList<GameNewsItemDto>?> TryGetCachedAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            return string.IsNullOrWhiteSpace(cached)
                ? null
                : JsonSerializer.Deserialize<List<GameNewsItemDto>>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read game news cache key {CacheKey}", cacheKey);
            return null;
        }
    }

    private async Task TrySetCachedAsync(string cacheKey, IReadOnlyList<GameNewsItemDto> items, CancellationToken cancellationToken)
    {
        try
        {
            var ttl = TimeSpan.FromHours(Math.Max(_options.CacheTtlHours, 1));
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(items, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write game news cache key {CacheKey}", cacheKey);
        }
    }

    private static DateTime? ParseRssDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var date)
            ? date.UtcDateTime
            : null;
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = Regex.Replace(decoded, "<.*?>", " ");
        var withoutBbCode = Regex.Replace(withoutTags, @"\[[^\]]+\]", " ");
        var normalized = Regex.Replace(withoutBbCode, @"\s+", " ").Trim();
        return normalized.Length <= 700 ? normalized : $"{normalized[..697]}...";
    }

    private sealed class SteamNewsResponse
    {
        [JsonPropertyName("appnews")]
        public SteamAppNews AppNews { get; set; } = new();
    }

    private sealed class SteamAppNews
    {
        [JsonPropertyName("newsitems")]
        public List<SteamNewsItem>? NewsItems { get; set; }
    }

    private sealed class SteamNewsItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("contents")]
        public string? Contents { get; set; }

        [JsonPropertyName("feedlabel")]
        public string? FeedLabel { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }
    }
}
