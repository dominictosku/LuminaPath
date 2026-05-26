using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos
{
    public class MySeriesDto : IBasicInfo
    {
        public int Id { get; set; }

        [Range(1, 10)]
        public byte? Rating { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public MediaStatus Status { get; set; }

        public double? TimeSpend { get; set; }

        public int SeriesId { get; set; }

        public SeriesNoIncludeDto? Series { get; set; }

        public int? CurrentWatchTimeMinutes { get; set; }

        public int? CurrentEpisode { get; set; }
    }
}
