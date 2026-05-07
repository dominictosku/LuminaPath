namespace LuminaPath.Core.Models
{
    public class DirectMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public LuminaUser? Sender { get; set; }
        public string RecipientId { get; set; } = string.Empty;
        public LuminaUser? Recipient { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
