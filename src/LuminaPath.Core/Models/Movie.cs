using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models
{
    public class Movie : Media
    {
        [Display(Name = "Expected watch time")]
        public int? ExpectedWatchTimeMinutes { get; set; }

        public List<MyMovie>? MyMovies { get; set; }
    }
}
