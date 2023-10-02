using Data.Models.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public partial class MyGame : MyMedia
	{
		[Required(ErrorMessage = "No {0} was choosen")]
		[Display(Name = "Game")]
		public int GameId { get; set; }
		public Game? Game { get; set; }
	}
}
