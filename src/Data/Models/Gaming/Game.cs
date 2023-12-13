using Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Gaming
{
	[Flags]
	public enum Plattforms
	{
		[Display(Name = "Playstation")]
		Playstation = 1,
		[Display(Name = "Switch")]
		Switch = 2,
		[Display(Name = "PC")]
		PC = 4,
		[Display(Name = "XBOX")]
		XBOX = 8
	}
	public partial class Game : Media
	{
		[Display(Name = "Plattform")]
		public Plattforms Plattforms { get; set; }
		[Display(Name = "Estimated playtime")]
		public int? Playtime { get; set; }
		public List<MyGame>? MyGames { get; set; }
	}
}
