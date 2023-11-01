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
			entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.ReleaseDate), includes);
			return await CreatePaginatedList(entities, paging);
		}
	}
}
