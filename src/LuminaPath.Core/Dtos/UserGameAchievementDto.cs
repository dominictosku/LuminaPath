using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Dtos;

public class UserGameAchievementDto
{
    public int Id { get; set; }
    public int GameAchievementId { get; set; }
    public ExternalMediaProvider Provider { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SourceAchievementId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsHidden { get; set; }
    public string? TrophyType { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public DateTime SyncedAt { get; set; }
}
