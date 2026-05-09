using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuminaPath.Core.Models
{
    public class MyMovie : MyMedia, IMyMedia
    {
        [NotMapped]
        public int MediaId => MovieId;

        public MediaStatus Status { get; set; }

        [Required(ErrorMessage = "No {0} was choosen")]
        [Display(Name = "Movie Id")]
        public int MovieId { get; set; }

        public Movie? Movie { get; set; }

        public int? CurrentWatchTimeMinutes { get; set; }
    }
}
