using Data.Models.Base;
using Data.Models.Gaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models.Quests
{
	public class GamesQuest : Quest
	{
		public Game? Games { get; set; }
	}
}
