using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos
{
    public class MyMovieDto : IBasicInfo
    {
        public int Id { get; set; }

        [Range(1, 10)]
        public byte? Rating { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public MediaStatus Status { get; set; }

        public int? TimeSpend { get; set; }

        public int MovieId { get; set; }

        public MoviesNoIncludeDto? Movie { get; set; }

        public int? CurrentWatchTimeMinutes { get; set; }
    }
}
