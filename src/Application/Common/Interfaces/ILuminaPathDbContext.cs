using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces
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