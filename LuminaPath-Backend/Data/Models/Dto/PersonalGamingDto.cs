using Data.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models.Dto
{
	public class PersonalGamingDto : PersonalList
	{
		public int GameId { get; set; }
	}
}
