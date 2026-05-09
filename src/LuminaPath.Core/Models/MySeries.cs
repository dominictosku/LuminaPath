using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuminaPath.Core.Models
{
    public class MySeries : MyMedia, IMyMedia
    {
        [NotMapped]
        public int MediaId => SeriesId;

        public MediaStatus Status { get; set; }

        [Required(ErrorMessage = "No {0} was choosen")]
        [Display(Name = "Series Id")]
        public int SeriesId { get; set; }

        public Series? Series { get; set; }

        public int? CurrentWatchTimeMinutes { get; set; }

        public int? CurrentEpisode { get; set; }

        public void RecalculateWatchTime(Series? series = null)
        {
            var resolvedSeries = series ?? Series;
            var currentEpisode = CurrentEpisode ?? 0;

            if (resolvedSeries?.EpisodeCount is > 0)
            {
                currentEpisode = Math.Min(currentEpisode, resolvedSeries.EpisodeCount.Value);
            }

            currentEpisode = Math.Max(0, currentEpisode);
            CurrentEpisode = currentEpisode;

            if (resolvedSeries?.ExpectedWatchTimePerEpisodeMinutes is not int minutesPerEpisode)
            {
                CurrentWatchTimeMinutes = null;
                TimeSpend = null;
                return;
            }

            CurrentWatchTimeMinutes = currentEpisode * minutesPerEpisode;
            TimeSpend = CurrentWatchTimeMinutes / 60d;
        }
    }
}
