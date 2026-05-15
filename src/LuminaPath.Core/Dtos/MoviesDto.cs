using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos
{
    public class MoviesDto : IBasicInfo
    {
        public int Id { get; set; }

        [Display(Name = "Title")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Genre { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release Date")]
        public DateTime? ReleaseDate { get; set; }

        [Display(Name = "Expected watch time")]
        public int? ExpectedWatchTimeMinutes { get; set; }

        public Document? Image { get; set; }

        public MyMovieDto? MyMovies { get; set; }
    }

    public class MoviesNoIncludeDto : IBasicInfo
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

        [Display(Name = "Expected watch time")]
        public int? ExpectedWatchTimeMinutes { get; set; }

        public string Source { get; set; } = string.Empty;

        public Document? Image { get; set; }
    }
}
