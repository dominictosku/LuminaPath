using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class QuestProfile : IBasicInfo
    {
        public int Id { get; set; }
        public int TotalXp { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string LuminaUserId { get; set; } = string.Empty;
        public LuminaUser? LuminaUser { get; set; }
    }
}
