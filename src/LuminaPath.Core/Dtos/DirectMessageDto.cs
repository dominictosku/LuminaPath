namespace LuminaPath.Core.Dtos
{
    public class DirectMessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class SendDirectMessageDto
    {
        public string RecipientId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
