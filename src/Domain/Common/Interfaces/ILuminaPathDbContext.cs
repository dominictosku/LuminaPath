using Domain.Models.Base;
using Domain.Models.Gaming;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;

namespace Domain.Common.Interfaces
{
	public interface ILuminaPathDbContext
	{
		DbSet<Game> Games { get; set; }
		DbSet<GamesQuest> GamesQuests { get; set; }
		DbSet<MyGame> MyGames { get; set; }
		DbSet<Quest> Quests { get; set; }
	}
}