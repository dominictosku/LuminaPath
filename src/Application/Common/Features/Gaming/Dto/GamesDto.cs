using Application.Common.Validation;
using Domain.Common.Enums;
using Domain.Common.Interfaces;
using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Application.Common.Features.Gaming.Dto
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

		public MediaFile? Image { get; set; }

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

		public MediaFile? Image { get; set; }
	}
}
