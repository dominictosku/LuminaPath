using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models.Base;
using Microsoft.AspNetCore.Http;

namespace Data.Models.Gaming
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
