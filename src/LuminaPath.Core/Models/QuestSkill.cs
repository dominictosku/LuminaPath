using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class QuestSkill : IBasicInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "code-slash-outline";
        public string Color { get; set; } = "#2563eb";
        public int Xp { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }
        public string LuminaUserId { get; set; } = string.Empty;
        public LuminaUser? LuminaUser { get; set; }
        public List<QuestSkillNode> Nodes { get; set; } = [];
    }
}
