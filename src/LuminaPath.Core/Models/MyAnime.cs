using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuminaPath.Core.Models
{
    public class MyAnime : MyMedia, IMyMedia
    {
        [NotMapped]
        public int MediaId => AnimeId;

        public MediaStatus Status { get; set; }

        [Required(ErrorMessage = "No {0} was choosen")]
        [Display(Name = "Anime Id")]
        public int AnimeId { get; set; }

        public Anime? Anime { get; set; }

        public int? CurrentWatchTimeMinutes { get; set; }

        public int? CurrentEpisode { get; set; }
    }
}
