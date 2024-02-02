using Domain.Entities;
using Domain.Models;
using Domain.Models.Gaming;
using Infrastructure.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class MyGameRepository : GenericRepository<MyGame>, IMyGameRepository
	{
		public UserManager<LuminaUser> UserManager { get; set; }
		public MyGameRepository(LuminaPathDbContext context, UserManager<LuminaUser> userManager) : base(context)
		{
			UserManager = userManager;
		}
		public async Task<PaginatedList<MyGame>> GetAllPaginated(
			Paging paging,
			string UserId,
			Expression<Func<MyGame, bool>> filter = null,
			IEnumerable<string> includes = null)
		{
			int pageIndex = paging.PageIndex;
			IQueryable<MyGame> entities = _entities.Where(g => g.LuminaUserId == UserId).Include(g => g.Game);
			entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.Game.ReleaseDate), includes);
			return await CreatePaginatedList(entities, paging);
		}
	}
}
