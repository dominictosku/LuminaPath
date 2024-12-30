using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Common.Validation;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.Third_Party;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Common.Features.Gaming.Dto
{
    public class GamesDto : IBasicInfo
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 2)]
        [UniqueName]
        [Display(Name = "Title")] public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Genre { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release Date")] public DateTime? ReleaseDate { get; set; }

        [Display(Name = "Plattform")] public Plattforms Plattforms { get; set; }

        [Display(Name = "Estimated playtime")] public int? Playtime { get; set; }

        public Document? Image { get; set; }

        public MyGameDto? MyGames { get; set; }

    }

    public class GamesNoIncludeDto : IBasicInfo
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Title")] public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Genre { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release Date")] public DateTime? ReleaseDate { get; set; }

        [Display(Name = "Plattform")] public Plattforms Plattforms { get; set; }

        [Display(Name = "Estimated playtime")] public int? Playtime { get; set; }

        public string Source { get; set; } = string.Empty;

        public MediaDocument? Image { get; set; }
        public GameInfo? GameInfo { get; set; }    
    }
}
