using Data.Classes;
using Data.Models.Dto;
using Data.Models.Gaming;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories
{
	public class GameRepo : GenericRepo<Game>
	{
		public GameRepo(LuminaPathDbContext context): base(context) 
		{
		}
		public async Task<PaginatedList<Game>> GetAllPaginated(MediaFIlter mediaFilter, string UserId, Expression<Func<Game, bool>> filter = null)
		{
			IQueryable<Game> games = _entities.Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
			if(filter != null)
			{
				games = games.Where(filter);
			}
			return await PaginatedList<Game>.CreateAsync(games, mediaFilter?.PageIndex ?? 1, 10);
		}

		public async Task<IEnumerable<Game>> GetAll(int? count, string UserId, Expression<Func<Game, bool>> filter = null)
		{
			IQueryable<Game> entities = _entities.Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
			if (filter != null)
			{
				entities = entities.Where(filter);
			}
			return await entities.Take(count ?? 100).ToListAsync();
		}
	}
}
