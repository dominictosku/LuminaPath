namespace LuminaPath.Core.Dtos
{
    public class FriendshipDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsIncoming { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public FriendUserDto User { get; set; } = new();
    }

    public class FriendUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
