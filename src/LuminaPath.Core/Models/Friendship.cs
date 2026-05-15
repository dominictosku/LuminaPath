namespace LuminaPath.Core.Models
{
    public enum FriendshipStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2
    }

    public class Friendship
    {
        public int Id { get; set; }
        public string RequesterId { get; set; } = string.Empty;
        public string AddresseeId { get; set; } = string.Empty;
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}
