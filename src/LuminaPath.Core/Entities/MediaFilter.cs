using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Entities
{
    public class MediaFilter
    {
        public string? SearchString { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Platforms? Platform { get; set; }
        public GameStatus? Status { get; set; }
        public MediaStatus? MediaStatus { get; set; }
        public string? Genre { get; set; }
        public string? Source { get; set; }
        public string? Publisher { get; set; }
        public int? MinPlaytime { get; set; }
        public int? MaxPlaytime { get; set; }
        public int? Priority { get; set; }
        public short? MinRating { get; set; }
        public double? MinPlayedHours { get; set; }
        public bool OnlyWithRemainingHours { get; set; }
        public string? Ownership { get; set; }
        public string? SortBy { get; set; }
        public string? SmartFilter { get; set; }
        public bool MyMedia { get; set; }
        public bool IncludeChildren { get; set; }
        public Paging Paging { get; set; } = new Paging();
    }
}
