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
    public partial class PersonalGaming : PersonalList
	{
		[Required(ErrorMessage = "No {0} was choosen")]
		[Display(Name = "Game")]
		public int GameId { get; set; }
		public Games? Game { get; set; }
		public string? LuminaUserId { get; set; }
		public LuminaUser? LuminaUser { get; set;}
	}
}
