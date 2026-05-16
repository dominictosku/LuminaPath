using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class QuestProfile : IBasicInfo
    {
        public int Id { get; set; }
        public int TotalXp { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public DateOnly? LastCompletionDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string LuminaUserId { get; set; } = string.Empty;

        public List<Achievement> Achievements { get; set; } = new();
    }
}
