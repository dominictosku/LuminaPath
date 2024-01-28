using Domain.Models.Base;
using Domain.Models.Gaming;

namespace Domain.Models.Quests
{
	public class GamesQuest : Quest
	{
		public Game? Games { get; set; }
	}
}
