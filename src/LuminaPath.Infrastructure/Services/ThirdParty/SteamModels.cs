using System.Text.Json.Serialization;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class SteamProfile
{
    public string SteamId { get; set; } = string.Empty;
    public string PersonaName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? ProfileUrl { get; set; }
}

public sealed class SteamOwnedGame
{
    public uint AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlaytimeMinutes { get; set; }
    public DateTime? LastPlayed { get; set; }
    public string? IconUrl { get; set; }
}

public sealed class SteamLibraryPreview
{
    public SteamProfile Profile { get; set; } = new();
    public int GameCount { get; set; }
    public List<SteamOwnedGame> Games { get; set; } = new();
}

public sealed class SteamImportResult
{
    public string SteamId { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
}

public sealed class SteamAchievementDefinition
{
    public string ApiName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? GrayIconUrl { get; set; }
    public bool Hidden { get; set; }
}

public sealed class SteamPlayerAchievement
{
    public string ApiName { get; set; } = string.Empty;
    public bool Achieved { get; set; }
    public DateTime? UnlockTime { get; set; }
}

// ---------- Steam Web API DTOs (private wire shapes) ----------

internal sealed class ResolveVanityResponse
{
    [JsonPropertyName("response")]
    public ResolveVanityPayload Response { get; set; } = new();
}

internal sealed class ResolveVanityPayload
{
    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal sealed class PlayerSummariesResponse
{
    [JsonPropertyName("response")]
    public PlayerSummariesPayload Response { get; set; } = new();
}

internal sealed class PlayerSummariesPayload
{
    [JsonPropertyName("players")]
    public List<PlayerSummary> Players { get; set; } = new();
}

internal sealed class PlayerSummary
{
    [JsonPropertyName("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [JsonPropertyName("personaname")]
    public string PersonaName { get; set; } = string.Empty;

    [JsonPropertyName("avatarmedium")]
    public string? AvatarMedium { get; set; }

    [JsonPropertyName("avatarfull")]
    public string? AvatarFull { get; set; }

    [JsonPropertyName("profileurl")]
    public string? ProfileUrl { get; set; }
}

internal sealed class OwnedGamesResponse
{
    [JsonPropertyName("response")]
    public OwnedGamesPayload Response { get; set; } = new();
}

internal sealed class OwnedGamesPayload
{
    [JsonPropertyName("game_count")]
    public int GameCount { get; set; }

    [JsonPropertyName("games")]
    public List<OwnedGame> Games { get; set; } = new();
}

internal sealed class OwnedGame
{
    [JsonPropertyName("appid")]
    public uint AppId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("playtime_forever")]
    public int PlaytimeForever { get; set; }

    [JsonPropertyName("rtime_last_played")]
    public long RtimeLastPlayed { get; set; }

    [JsonPropertyName("img_icon_url")]
    public string? ImgIconUrl { get; set; }
}

internal sealed class SteamSchemaResponse
{
    [JsonPropertyName("game")]
    public SteamSchemaGame Game { get; set; } = new();
}

internal sealed class SteamSchemaGame
{
    [JsonPropertyName("availableGameStats")]
    public SteamAvailableGameStats? AvailableGameStats { get; set; }
}

internal sealed class SteamAvailableGameStats
{
    [JsonPropertyName("achievements")]
    public List<SteamSchemaAchievement> Achievements { get; set; } = new();
}

internal sealed class SteamSchemaAchievement
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("icongray")]
    public string? IconGray { get; set; }

    [JsonPropertyName("hidden")]
    public int Hidden { get; set; }
}

internal sealed class SteamPlayerAchievementsResponse
{
    [JsonPropertyName("playerstats")]
    public SteamPlayerStats PlayerStats { get; set; } = new();
}

internal sealed class SteamPlayerStats
{
    [JsonPropertyName("achievements")]
    public List<SteamPlayerAchievementWire> Achievements { get; set; } = new();
}

internal sealed class SteamPlayerAchievementWire
{
    [JsonPropertyName("apiname")]
    public string ApiName { get; set; } = string.Empty;

    [JsonPropertyName("achieved")]
    public int Achieved { get; set; }

    [JsonPropertyName("unlocktime")]
    public long UnlockTime { get; set; }
}
