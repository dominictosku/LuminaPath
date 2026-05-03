using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Dtos
{
    public class QuestBoardDto
    {
        public int Xp { get; set; }
        public List<QuestDto> Quests { get; set; } = [];
        public List<QuestSkillDto> Skills { get; set; } = [];
    }

    public class QuestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public QuestType Type { get; set; } = QuestType.Sub;
        public int RewardXp { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }
    }

    public class QuestSkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "code-slash-outline";
        public string Color { get; set; } = "#2563eb";
        public int Xp { get; set; }
        public int SortOrder { get; set; }
        public List<QuestSkillNodeDto> Nodes { get; set; } = [];
    }

    public class QuestSkillNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Unlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public int SortOrder { get; set; }
    }
}
