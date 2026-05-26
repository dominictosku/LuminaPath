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

    public class FriendProfileDto
    {
        public FriendUserDto User { get; set; } = new();
        public FriendProfileStatsDto Stats { get; set; } = new();
        public List<FriendLibraryItemDto> NowPlaying { get; set; } = new();
        public List<FriendLibraryItemDto> RecentCompletions { get; set; } = new();
        public List<FriendActivityItemDto> Activity { get; set; } = new();
    }

    public class FriendProfileStatsDto
    {
        public int Games { get; set; }
        public int Animes { get; set; }
        public int Movies { get; set; }
        public int Series { get; set; }
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public int ActiveItems { get; set; }
        public double TotalTrackedHours { get; set; }
        public double? AverageRating { get; set; }
    }

    public class FriendLibraryItemDto
    {
        public string Kind { get; set; } = string.Empty;
        public int MediaId { get; set; }
        public int LibraryEntryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public short? Rating { get; set; }
        public double? TimeSpend { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class FriendActivityItemDto
    {
        public string Kind { get; set; } = string.Empty;
        public string Verb { get; set; } = string.Empty;
        public DateTime? OccurredAt { get; set; }
        public FriendLibraryItemDto Item { get; set; } = new();
    }
}
