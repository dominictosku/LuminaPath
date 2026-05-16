using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Models;

public class GameAchievement
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game? Game { get; set; }
    public string CanonicalKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsHidden { get; set; }
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    public string? SteamApiName { get; set; }
    public string? SteamDisplayName { get; set; }

    public int? PsnTrophyId { get; set; }
    public string? PsnGroupId { get; set; }
    public string? PsnTrophyType { get; set; }

    public ExternalMediaProvider? PrimaryProvider { get; set; }
    public List<UserGameAchievement> UserAchievements { get; set; } = new();
}
