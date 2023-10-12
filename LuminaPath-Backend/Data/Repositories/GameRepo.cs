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
		public GameRepo(LuminaPathDbContext context) : base(context)
		{
		}

		public async Task<PaginatedList<Game>> GetAllPaginated(
			Paging paging,
			string UserId,
			Expression<Func<Game, bool>> filter = null,
			IEnumerable<string> includes = null)
		{
			int pageIndex = paging.PageIndex;
			IQueryable<Game> entities = _entities.Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
			if (filter != null)
			{
				entities = entities.Where(filter);
			}
			if (includes != null)
			{
				entities = includes.Aggregate(_entities.AsQueryable(), (current, include) => current.Include(include));
			}
			if (paging.Count > 0)
			{
				return await PaginatedList<Game>.CreateAsync(entities, 1, paging.Count);
			}
			return await PaginatedList<Game>.CreateAsync(entities, pageIndex, 10);
		}
	}
}
