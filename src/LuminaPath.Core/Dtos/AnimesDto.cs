using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Validation;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos
{
    public class AnimesDto : IBasicInfo
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 2)]
        [UniqueName]
        [Display(Name = "Title")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Genre { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release Date")]
        public DateTime? ReleaseDate { get; set; }

        [Display(Name = "Expected watch time per episode")]
        public int? ExpectedWatchTimePerEpisodeMinutes { get; set; }

        public int? ExpectedWatchTimeMinutes { get; set; }

        public int? EpisodeCount { get; set; }

        public Document? Image { get; set; }

        public MyAnimeDto? MyAnimes { get; set; }
    }

    public class AnimesNoIncludeDto : IBasicInfo
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Title")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Genre { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release Date")]
        public DateTime? ReleaseDate { get; set; }

        [Display(Name = "Expected watch time per episode")]
        public int? ExpectedWatchTimePerEpisodeMinutes { get; set; }

        public int? ExpectedWatchTimeMinutes { get; set; }

        public int? EpisodeCount { get; set; }

        public string Source { get; set; } = string.Empty;

        public Document? Image { get; set; }
    }
}
