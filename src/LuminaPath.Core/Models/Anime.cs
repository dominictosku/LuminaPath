using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models
{
    public class Anime : Media
    {
        [Display(Name = "Expected watch time")]
        public int? ExpectedWatchTimeMinutes { get; set; }

        public int? EpisodeCount { get; set; }

        public List<MyAnime>? MyAnimes { get; set; }
    }
}
