using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Core.Common.Interfaces
{
	public interface ILuminaPathDbContext
	{
		DbSet<Game> Games { get; set; }
		DbSet<GamesQuest> GamesQuests { get; set; }
		DbSet<MyGame> MyGames { get; set; }
		DbSet<Quest> Quests { get; set; }
		Task<int> SaveChangesAsync(CancellationToken cancellationToken);
	}
}