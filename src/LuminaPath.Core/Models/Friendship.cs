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
        public LuminaUser? Requester { get; set; }
        public string AddresseeId { get; set; } = string.Empty;
        public LuminaUser? Addressee { get; set; }
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}
