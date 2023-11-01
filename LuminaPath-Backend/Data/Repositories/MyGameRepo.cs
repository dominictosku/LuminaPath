using Data.Classes;
using Data.Models;
using Data.Models.Dto;
using Data.Models.Gaming;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories
{
	public class MyGameRepo : GenericRepo<MyGame>
	{
		public UserManager<LuminaUser> UserManager { get; set; }
		public MyGameRepo(LuminaPathDbContext context, UserManager<LuminaUser> userManager): base(context) 
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
			entities = PrepareEntity(entities, filter, includes);
			return await CreatePaginatedList(entities, paging);
		}
	}
}
