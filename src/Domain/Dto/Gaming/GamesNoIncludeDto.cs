using Domain.Models.Base;
using Domain.Models.Gaming;
using System.ComponentModel.DataAnnotations;

namespace Domain.Dto.Gaming
{
	public class GamesNoIncludeDto : Media
	{
		[Display(Name = "Plattform")]
		public Plattforms Plattforms { get; set; }
		[Display(Name = "Estimated playtime")]
		public int? Playtime { get; set; }
	}
}
