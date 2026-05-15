using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models
{
    public class GamingSession : IBasicInfo
    {
        public int Id { get; set; }
        public string LuminaUserId { get; set; } = string.Empty;
        public int? MyGameId { get; set; }
        public MyGame? MyGame { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
