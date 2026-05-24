using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Dtos
{
    public class QuestBoardDto
    {
        public int Xp { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public DateTime? LastCompletionDate { get; set; }
        public List<QuestDto> Quests { get; set; } = [];
        public List<QuestSkillDto> Skills { get; set; } = [];
        public List<QuestFolderDto> Folders { get; set; } = [];
        public List<AchievementDto> Achievements { get; set; } = [];
    }

    public class QuestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public QuestType Type { get; set; } = QuestType.Sub;
        public QuestPriority Priority { get; set; } = QuestPriority.Medium;
        public QuestRecurrence Recurrence { get; set; } = QuestRecurrence.None;
        public DateTime? DueDate { get; set; }
        public List<string> Tags { get; set; } = [];
        public int RewardXp { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }
        public int? MyGameId { get; set; }
        public string? GameName { get; set; }
        public int? SkillId { get; set; }
        public string? SkillName { get; set; }
        public int? QuestFolderId { get; set; }
        public string? FolderName { get; set; }
        public string? FolderEmoji { get; set; }
        public List<QuestSubtaskDto> Subtasks { get; set; } = [];
    }

    public class QuestSubtaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int SortOrder { get; set; }
    }

    public class QuestSubtaskCreateDto
    {
        public string Title { get; set; } = string.Empty;
    }

    public class QuestSubtaskUpdateDto
    {
        public string? Title { get; set; }
        public bool? Completed { get; set; }
        public int? SortOrder { get; set; }
    }

    public class QuestCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public QuestType Type { get; set; } = QuestType.Sub;
        public QuestPriority Priority { get; set; } = QuestPriority.Medium;
        public QuestRecurrence Recurrence { get; set; } = QuestRecurrence.None;
        public DateTime? DueDate { get; set; }
        public List<string> Tags { get; set; } = [];
        public int? MyGameId { get; set; }
        public int? SkillId { get; set; }
        public int? QuestFolderId { get; set; }
    }

    public class QuestUpdateDto
    {
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public QuestType? Type { get; set; }
        public QuestPriority? Priority { get; set; }
        public QuestRecurrence? Recurrence { get; set; }
        public DateTime? DueDate { get; set; }
        public bool? ClearDueDate { get; set; }
        public List<string>? Tags { get; set; }
        public bool? Completed { get; set; }
        public int? MyGameId { get; set; }
        public bool? ClearMyGame { get; set; }
        public int? SortOrder { get; set; }
        public int? SkillId { get; set; }
        public bool? ClearSkill { get; set; }
        public int? QuestFolderId { get; set; }
        public bool? ClearQuestFolder { get; set; }
    }

    public class QuestFolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? SectionName { get; set; }
        public int SortOrder { get; set; }
    }

    public class QuestFolderCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? SectionName { get; set; }
    }

    public class QuestFolderUpdateDto
    {
        public string? Name { get; set; }
        public string? Emoji { get; set; }
        public string? Color { get; set; }
        public bool? ClearColor { get; set; }
        public string? SectionName { get; set; }
        public bool? ClearSectionName { get; set; }
        public int? SortOrder { get; set; }
    }

    public class QuestReorderItemDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public QuestType Type { get; set; } = QuestType.Sub;
    }

    public class QuestMutationResultDto
    {
        public QuestDto Quest { get; set; } = new();
        public QuestDto? SpawnedQuest { get; set; }
        public int TotalXp { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public int? AwardedSkillXp { get; set; }
        public int? AwardedSkillId { get; set; }
        public List<AchievementDto> UnlockedAchievements { get; set; } = [];
    }

    public class AchievementDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; }
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
