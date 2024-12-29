using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.Third_Party;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models
{
	public class Game : Media
	{
		[Display(Name = "Plattform")]
		public Plattforms Plattforms { get; set; }
		[Display(Name = "Estimated playtime")]
		public int? Playtime { get; set; }
		public List<MyGame>? MyGames { get; set; }
		public GameInfo? GameInfo { get; set; }
	}
}
