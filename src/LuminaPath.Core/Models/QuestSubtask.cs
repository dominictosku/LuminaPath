namespace LuminaPath.Core.Models
{
    public class QuestSubtask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }

        public int QuestId { get; set; }
        public Quest? Quest { get; set; }
    }
}
