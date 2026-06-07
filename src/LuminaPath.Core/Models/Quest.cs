using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class Quest : IBasicInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public QuestType Type { get; set; } = QuestType.Sub;
        public QuestPriority Priority { get; set; } = QuestPriority.Medium;
        public QuestRecurrence Recurrence { get; set; } = QuestRecurrence.None;
        public DateTime? DueDate { get; set; }
        public DateTime? ScheduledStartAt { get; set; }
        public DateTime? ScheduledEndAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public int RewardXp { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }
        public string LuminaUserId { get; set; } = string.Empty;
        public int? MyGameId { get; set; }
        public MyGame? MyGame { get; set; }
        public int? SkillId { get; set; }
        public QuestSkill? Skill { get; set; }
        public int? QuestFolderId { get; set; }
        public QuestFolder? QuestFolder { get; set; }
        public List<QuestSubtask> Subtasks { get; set; } = new();
    }
}
