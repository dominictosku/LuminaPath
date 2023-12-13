using Core.Interfaces;
using Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models.Gaming
{
	public partial class MyGame : MyMedia, IMyMedia
	{
		[Required(ErrorMessage = "No {0} was choosen")]
		[Display(Name = "Game")]
		[NotMapped]
		public int MediaId => GameId;
		public int GameId { get; set; }
		public Game? Game { get; set; }
	}
}
