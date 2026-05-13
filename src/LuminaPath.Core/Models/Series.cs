using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuminaPath.Core.Models
{
    public class Series : Media
    {
        [Display(Name = "Expected watch time per episode")]
        public int? ExpectedWatchTimePerEpisodeMinutes { get; set; }

        public int? EpisodeCount { get; set; }

        [NotMapped]
        public int? ExpectedWatchTimeMinutes =>
            EpisodeCount is null || ExpectedWatchTimePerEpisodeMinutes is null
                ? null
                : EpisodeCount.Value * ExpectedWatchTimePerEpisodeMinutes.Value;

        public int? ParentSeriesId { get; set; }
        public Series? ParentSeries { get; set; }
        public List<Series>? Seasons { get; set; }

        public List<MySeries>? MySeries { get; set; }
    }
}
