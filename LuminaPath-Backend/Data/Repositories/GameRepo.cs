using Data.Classes;
using Data.Models;
using Data.Models.Dto;
using Data.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories
{
	public class GameRepo : GenericRepo<Game>
	{
		public GameRepo(LuminaPathDbContext context): base(context) 
		{
		}
		public async Task<PaginatedList<Game>> GetAllPaginated(MediaFIlter filter, string UserId)
		{
			var games = _entities.Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
			return await PaginatedList<Game>.CreateAsync(games, filter?.PageIndex ?? 1, 10);
		}
	}
}
