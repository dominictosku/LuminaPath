using Application.Common.Interfaces.Repositories;
using Domain.Common.Entities;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
	public class GameRepository : GenericRepository<Game>, IGameRepository
	{
		public GameRepository(LuminaPathDbContext context) : base(context)
		{
		}

		public async Task<PaginatedList<Game>> GetAllPaginated(
			Paging paging,
			string UserId,
			Expression<Func<Game, bool>> filter = null,
			IEnumerable<string> includes = null)
		{
			int pageIndex = paging.PageIndex;
			IQueryable<Game> entities = _entities.Include(g => g.Image).Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
			entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.ReleaseDate), includes);
			return await CreatePaginatedList(entities, paging);
		}
	}
}
