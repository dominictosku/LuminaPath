using System.Net.Http.Json;
using System.Text.RegularExpressions;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Third_Party;

public sealed class SteamService
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
    private readonly ILogger<SteamService> _logger;

    public SteamService(
        HttpClient http,
        IOptions<SteamOptions> options,
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        ApplicationSettingsService settings,
        ILogger<SteamService> logger)
    {
        _http = http;
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _settings = settings;
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

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await UpdateUserSteamProfileAsync(context, user.Id, steamId, profile, cancellationToken);

        var appIds = games.Select(g => g.AppId.ToString()).ToHashSet();
        var existingGames = await context.Games
            .Include(g => g.GameInfo)
            .Include(g => g.MyGames!)
                .ThenInclude(m => m.MyGameInfo)
            .Where(g => g.GameInfo != null && g.GameInfo.SteamId != null && appIds.Contains(g.GameInfo.SteamId!))
            .ToListAsync(cancellationToken);

        var existingByAppId = existingGames.ToDictionary(g => g.GameInfo!.SteamId!, StringComparer.Ordinal);

        var importedNames = games.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var nameClashes = await context.Games
            .Where(g => importedNames.Contains(g.Name))
            .Select(g => g.Name)
            .ToListAsync(cancellationToken);
        var existingNames = new HashSet<string>(nameClashes, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        var newGames = new List<Game>();

        foreach (var owned in games)
        {
            var appIdString = owned.AppId.ToString();
            var trackedHours = Math.Round(owned.PlaytimeMinutes / 60.0, 2);
            var lastPlayed = owned.LastPlayed;

            if (existingByAppId.TryGetValue(appIdString, out var existing))
            {
                ApplyToExisting(existing, user, trackedHours, lastPlayed);
                updated++;
            }
            else
            {
                var name = ResolveUniqueName(owned.Name, existingNames, appIdString);
                existingNames.Add(name);
                newGames.Add(BuildNewGame(name, appIdString, user, trackedHours, lastPlayed));
                added++;
            }
        }

        if (newGames.Count > 0)
        {
            await context.Games.AddRangeAsync(newGames, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new SteamImportResult
        {
            SteamId = steamId,
            Total = games.Count,
            Added = added,
            Updated = updated,
        };
    }

    private static void ApplyToExisting(
        Game existing,
        LuminaUser user,
        double trackedHours,
        DateTime? lastPlayed)
    {
        if ((existing.Platforms & Platforms.PC) == 0)
        {
            existing.Platforms |= Platforms.PC;
        }

        var userGame = existing.MyGames?.FirstOrDefault(m => m.LuminaUserId == user.Id);
        if (userGame is null)
        {
            existing.MyGames ??= new List<MyGame>();
            existing.MyGames.Add(new MyGame
            {
                GameId = existing.Id,
                LuminaUserId = user.Id,
                MyGameInfo = new MyGameInfo
                {
                    TrackedHours = trackedHours,
                    FirstPlayed = lastPlayed ?? default,
                    LastPlayed = lastPlayed ?? default,
                },
            });
            return;
        }

        if (userGame.MyGameInfo is null)
        {
            userGame.MyGameInfo = new MyGameInfo
            {
                TrackedHours = trackedHours,
                FirstPlayed = lastPlayed ?? default,
                LastPlayed = lastPlayed ?? default,
            };
        }
        else
        {
            userGame.MyGameInfo.TrackedHours = trackedHours;
            if (lastPlayed.HasValue && (userGame.MyGameInfo.FirstPlayed == default || userGame.MyGameInfo.FirstPlayed > lastPlayed.Value))
            {
                userGame.MyGameInfo.FirstPlayed = lastPlayed.Value;
            }
            if (lastPlayed.HasValue)
            {
                userGame.MyGameInfo.LastPlayed = lastPlayed.Value;
            }
        }
    }

    private static Game BuildNewGame(
        string name,
        string steamAppId,
        LuminaUser user,
        double trackedHours,
        DateTime? lastPlayed)
    {
        return new Game
        {
            Name = name,
            Platforms = Platforms.PC,
            Source = "Steam",
            GameInfo = new GameInfo
            {
                SteamId = steamAppId,
            },
            MyGames = new List<MyGame>
            {
                new()
                {
                    LuminaUserId = user.Id,
                    MyGameInfo = new MyGameInfo
                    {
                        TrackedHours = trackedHours,
                        FirstPlayed = lastPlayed ?? default,
                        LastPlayed = lastPlayed ?? default,
                    },
                },
            },
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

    private static string ResolveUniqueName(string desired, HashSet<string> taken, string steamAppId)
    {
        if (!taken.Contains(desired)) return desired;
        return $"{desired} (Steam {steamAppId})";
    }
}
