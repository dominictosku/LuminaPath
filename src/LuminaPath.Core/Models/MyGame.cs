using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuminaPath.Core.Models
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
