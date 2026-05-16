namespace LuminaPath.Core.Models
{
    public class Achievement
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        public int QuestProfileId { get; set; }
        public QuestProfile? QuestProfile { get; set; }
    }
}
