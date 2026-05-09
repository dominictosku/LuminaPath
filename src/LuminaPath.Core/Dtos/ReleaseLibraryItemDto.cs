using LuminaPath.Core.Models.Base;

namespace LuminaPath.Core.Dtos
{
    public class ReleaseLibraryItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Genre { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public string Kind { get; set; } = string.Empty;

        public int AddedCount { get; set; }

        public Document? Image { get; set; }

        public int? Platforms { get; set; }

        public int? Playtime { get; set; }

        public int? EpisodeCount { get; set; }

        public int? ExpectedWatchTimePerEpisodeMinutes { get; set; }

        public int? ExpectedWatchTimeMinutes { get; set; }

        public ReleaseLibraryUserEntryDto? LibraryEntry { get; set; }
    }

    public class ReleaseLibraryUserEntryDto
    {
        public int Id { get; set; }

        public short? Rating { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int Status { get; set; }

        public double? TimeSpend { get; set; }

        public ReleaseLibraryGameInfoDto? MyGameInfo { get; set; }

        public int? CurrentWatchTimeMinutes { get; set; }

        public int? CurrentEpisode { get; set; }
    }

    public class ReleaseLibraryGameInfoDto
    {
        public double? TrackedHours { get; set; }
    }
}
