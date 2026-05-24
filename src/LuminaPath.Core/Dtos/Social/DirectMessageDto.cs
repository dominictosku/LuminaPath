namespace LuminaPath.Core.Dtos
{
    public static class DirectMessageLimits
    {
        public const int MaxRequestBytes = 8 * 1024;
        public const int MaxMessageCharacters = 4_000;
        public const int MaxConversationTake = 100;
    }

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
