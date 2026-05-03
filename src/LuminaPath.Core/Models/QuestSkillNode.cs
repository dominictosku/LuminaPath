using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class QuestSkillNode : IBasicInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Unlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public int SortOrder { get; set; }
        public int QuestSkillId { get; set; }
        public QuestSkill? QuestSkill { get; set; }
    }
}
