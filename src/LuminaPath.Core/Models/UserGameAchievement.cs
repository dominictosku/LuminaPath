using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Models;

public class UserGameAchievement
{
    public int Id { get; set; }
    public int GameAchievementId { get; set; }
    public GameAchievement? GameAchievement { get; set; }
    public string LuminaUserId { get; set; } = string.Empty;
    public ExternalMediaProvider Provider { get; set; }
    public string SourceAchievementId { get; set; } = string.Empty;
    public DateTime? UnlockedAt { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
