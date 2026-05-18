using System.Net.Http.Json;
using System.Text.RegularExpressions;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class SteamService : ISteamAchievementClient
{
    private static readonly Regex ProfileUrlRegex = new(
        @"steamcommunity\.com/(?:id|profiles)/([^/?#]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SteamId64Regex = new(@"^7656119\d{10}$", RegexOptions.Compiled);
    private static readonly Regex VanityRegex = new(@"^[A-Za-z0-9_-]{2,32}$", RegexOptions.Compiled);

    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly HttpClient _http;
    private readonly SteamOptions _options;
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly ApplicationSettingsService _settings;
    private readonly GameImportPipeline _importPipeline;
    private readonly ILogger<SteamService> _logger;

    public SteamService(
        HttpClient http,
        IOptions<SteamOptions> options,
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        ApplicationSettingsService settings,
        GameImportPipeline importPipeline,
        ILogger<SteamService> logger)
    {
        _http = http;
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _settings = settings;
        _importPipeline = importPipeline;
        _logger = logger;
    }

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        return _settings.HasSteamApiKeyAsync(cancellationToken);
    }

    /// <summary>
    /// Accepts a 17-digit steamID64, a vanity name (the segment after /id/), or a full
    /// steamcommunity.com profile URL. Returns the canonical steamID64 or null if it
    /// can't be resolved.
    /// </summary>
    public async Task<string?> ResolveSteamIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return null;
        var input = identifier.Trim();

        // Full URL? Pull out the slug.
        var urlMatch = ProfileUrlRegex.Match(input);
        if (urlMatch.Success)
        {
            input = urlMatch.Groups[1].Value;
        }

        if (SteamId64Regex.IsMatch(input))
        {
            return input;
        }

        if (!VanityRegex.IsMatch(input))
        {
            return null;
        }

        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/ISteamUser/ResolveVanityURL/v1/?key={apiKey}&vanityurl={Uri.EscapeDataString(input)}";

        try
        {
            var resolved = await _http.GetFromJsonAsync<ResolveVanityResponse>(url, cancellationToken);
            if (resolved?.Response.Success == 1 && !string.IsNullOrWhiteSpace(resolved.Response.SteamId))
            {
                return resolved.Response.SteamId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve Steam vanity URL '{Vanity}'", input);
        }

        return null;
    }

    public async Task<SteamProfile?> GetProfileAsync(string steamId, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId)) return null;

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId}";

        try
        {
            var data = await _http.GetFromJsonAsync<PlayerSummariesResponse>(url, cancellationToken);
            var player = data?.Response.Players.FirstOrDefault();
            if (player is null) return null;

            return new SteamProfile
            {
                SteamId = player.SteamId,
                PersonaName = player.PersonaName,
                AvatarUrl = player.AvatarFull ?? player.AvatarMedium,
                ProfileUrl = player.ProfileUrl,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam profile for {SteamId}", steamId);
            return null;
        }
    }

    public async Task<List<SteamOwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId)) return new List<SteamOwnedGame>();

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=true&include_played_free_games=true";

        try
        {
            var data = await _http.GetFromJsonAsync<OwnedGamesResponse>(url, cancellationToken);
            if (data?.Response.Games is null) return new List<SteamOwnedGame>();

            return data.Response.Games
                .Where(g => g.AppId > 0 && !string.IsNullOrWhiteSpace(g.Name))
                .Select(g => new SteamOwnedGame
                {
                    AppId = g.AppId,
                    Name = g.Name,
                    PlaytimeMinutes = g.PlaytimeForever,
                    LastPlayed = g.RtimeLastPlayed > 0 ? UnixEpoch.AddSeconds(g.RtimeLastPlayed) : null,
                    IconUrl = !string.IsNullOrWhiteSpace(g.ImgIconUrl)
                        ? $"https://media.steampowered.com/steamcommunity/public/images/apps/{g.AppId}/{g.ImgIconUrl}.jpg"
                        : null,
                })
                .OrderByDescending(g => g.PlaytimeMinutes)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam owned games for {SteamId} (profile may be private)", steamId);
            return new List<SteamOwnedGame>();
        }
    }

    public async Task<SteamLibraryPreview?> PreviewLibraryAsync(string identifier, CancellationToken cancellationToken)
    {
        var steamId = await ResolveSteamIdAsync(identifier, cancellationToken);
        if (steamId is null) return null;

        var profile = await GetProfileAsync(steamId, cancellationToken);
        var games = await GetOwnedGamesAsync(steamId, cancellationToken);

        return new SteamLibraryPreview
        {
            Profile = profile ?? new SteamProfile { SteamId = steamId, PersonaName = steamId },
            GameCount = games.Count,
            Games = games.Take(12).ToList(),
        };
    }

    public async Task<List<SteamAchievementDefinition>> GetAchievementSchemaAsync(uint appId, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || appId == 0) return new List<SteamAchievementDefinition>();

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&appid={appId}&l=english";

        try
        {
            var data = await _http.GetFromJsonAsync<SteamSchemaResponse>(url, cancellationToken);
            return data?.Game.AvailableGameStats?.Achievements
                .Where(achievement => !string.IsNullOrWhiteSpace(achievement.Name))
                .Select(achievement => new SteamAchievementDefinition
                {
                    ApiName = achievement.Name,
                    DisplayName = string.IsNullOrWhiteSpace(achievement.DisplayName) ? achievement.Name : achievement.DisplayName,
                    Description = achievement.Description,
                    IconUrl = achievement.Icon,
                    GrayIconUrl = achievement.IconGray,
                    Hidden = achievement.Hidden == 1,
                })
                .ToList() ?? new List<SteamAchievementDefinition>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam achievement schema for app {AppId}", appId);
            return new List<SteamAchievementDefinition>();
        }
    }

    public async Task<List<SteamPlayerAchievement>> GetPlayerAchievementsAsync(string steamId, uint appId, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId) || appId == 0)
        {
            return new List<SteamPlayerAchievement>();
        }

        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/ISteamUserStats/GetPlayerAchievements/v1/?key={apiKey}&steamid={steamId}&appid={appId}&l=english";

        try
        {
            var data = await _http.GetFromJsonAsync<SteamPlayerAchievementsResponse>(url, cancellationToken);
            return data?.PlayerStats.Achievements
                .Where(achievement => !string.IsNullOrWhiteSpace(achievement.ApiName))
                .Select(achievement => new SteamPlayerAchievement
                {
                    ApiName = achievement.ApiName,
                    Achieved = achievement.Achieved == 1,
                    UnlockTime = achievement.UnlockTime > 0 ? UnixEpoch.AddSeconds(achievement.UnlockTime) : null,
                })
                .ToList() ?? new List<SteamPlayerAchievement>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Steam player achievements for {SteamId} app {AppId}", steamId, appId);
            return new List<SteamPlayerAchievement>();
        }
    }

    public async Task<SteamImportResult> ImportLibraryAsync(LuminaUser user, string identifier, CancellationToken cancellationToken)
    {
        var steamId = await ResolveSteamIdAsync(identifier, cancellationToken);
        if (steamId is null)
        {
            return new SteamImportResult();
        }

        var profile = await GetProfileAsync(steamId, cancellationToken);
        var games = await GetOwnedGamesAsync(steamId, cancellationToken);
        if (games.Count == 0)
        {
            await SaveUserSteamProfileAsync(user.Id, steamId, profile, cancellationToken);
            return new SteamImportResult { SteamId = steamId };
        }

        await SaveUserSteamProfileAsync(user.Id, steamId, profile, cancellationToken);
        var importResult = await _importPipeline.ImportAsync(
            user,
            games.Select(owned => new GameImportItem
            {
                Name = owned.Name,
                Source = "Steam",
                Platforms = Platforms.PC,
                ExternalProvider = ExternalMediaProvider.Steam,
                ExternalId = owned.AppId.ToString(),
                TrackedHours = Math.Round(owned.PlaytimeMinutes / 60.0, 2),
                FirstPlayed = owned.LastPlayed,
                LastPlayed = owned.LastPlayed,
            }),
            cancellationToken);

        return new SteamImportResult
        {
            SteamId = steamId,
            Total = games.Count,
            Added = importResult.CreatedGames,
            Updated = importResult.UpdatedGames + importResult.UpdatedMyGames,
        };
    }

    private async Task SaveUserSteamProfileAsync(
        string userId,
        string steamId,
        SteamProfile? profile,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await UpdateUserSteamProfileAsync(context, userId, steamId, profile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpdateUserSteamProfileAsync(
        LuminaPathDbContext context,
        string userId,
        string steamId,
        SteamProfile? profile,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.LuminaUserInfo)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return;
        }

        user.LuminaUserInfo ??= new LuminaUserInfo { UserId = user.Id };
        user.LuminaUserInfo.SteamId = steamId;
        user.LuminaUserInfo.SteamPersonaName = profile?.PersonaName ?? user.LuminaUserInfo.SteamPersonaName;
        user.LuminaUserInfo.SteamProfileUrl = profile?.ProfileUrl ?? user.LuminaUserInfo.SteamProfileUrl;
        user.LuminaUserInfo.SteamAvatarUrl = profile?.AvatarUrl ?? user.LuminaUserInfo.SteamAvatarUrl;
    }

}
