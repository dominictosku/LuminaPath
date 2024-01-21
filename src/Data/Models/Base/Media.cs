using Core.Entities.Validation;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Base
{
    [Index(nameof(Name), IsUnique = true)]
	public abstract class Media : IBasicInfo
	{
		public int Id { get; set; }

		[Required]
		[StringLength(50)]
		[UniqueName]
		[Display(Name = "Title")]
		public string? Name { get; set; }

		public string? Description { get; set; }

		public string? Genre { get; set; }

		[DataType(DataType.Date)]
		[Display(Name = "Release Date")]
		public DateTime? ReleaseDate { get; set; }

		public MediaFile? Image { get; set; }
	}
}
