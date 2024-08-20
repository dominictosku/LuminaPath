using LuminaPath.Core.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models.Base
{
	[Index(nameof(Name), IsUnique = true)]
	public abstract class Media : IMedia<Document>
	{
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false)]
		[StringLength(50, MinimumLength = 2)]
		[Display(Name = "Title")] public string Name { get; set; } = string.Empty;

		public string? Description { get; set; }

		public string? Genre { get; set; }

		[DataType(DataType.Date)]
		[Display(Name = "Release Date")] public DateTime? ReleaseDate { get; set; }

		public Document? Image { get; set; }
	}
}
