using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class Quest : IBasicInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public QuestType Type { get; set; } = QuestType.Sub;
        public int RewardXp { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }
        public string LuminaUserId { get; set; } = string.Empty;
        public LuminaUser? LuminaUser { get; set; }
        public int? MyGameId { get; set; }
        public MyGame? MyGame { get; set; }
    }
}
