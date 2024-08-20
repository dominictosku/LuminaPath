using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Common.Interfaces;

namespace LuminaPath.MauiClientApp.Models
{
	public class LocalGame
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false)]
		[StringLength(50, MinimumLength = 2)]
		[Display(Name = "Title")] public string Name { get; set; } = string.Empty;

		public string? Description { get; set; }

		public string? Genre { get; set; }

		[DataType(DataType.Date)]
		[Display(Name = "Release Date")] public DateTime? ReleaseDate { get; set; }

		[Display(Name = "Plattform")]
		public Plattforms Plattforms { get; set; }
		[Display(Name = "Estimated playtime")]
		public int? Playtime { get; set; }
	}
}
