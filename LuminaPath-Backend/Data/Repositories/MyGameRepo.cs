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
		public async Task<PaginatedList<MyGame>> GetAllPaginated(MediaFIlter mediaFilter, string UserId, Expression<Func<MyGame, bool>> filter = null)
		{
			IQueryable<MyGame> myGames = _entities.Where(g => g.LuminaUserId == UserId).Include(g => g.Game);
			if (filter != null)
			{
				myGames = myGames.Where(filter);
			}
			return await PaginatedList<MyGame>.CreateAsync(myGames, mediaFilter?.PageIndex ?? 1, 10);
		}

		public async Task<List<MyGame>> GetAll(int count, string UserId, Expression<Func<MyGame, bool>> filter = null)
		{
			IQueryable<MyGame> myGames = _entities.Where(g => g.LuminaUserId == UserId).Include(g => g.Game);
			if (filter != null)
			{
				myGames = myGames.Where(filter);
			}
			return await myGames.Take(count).ToListAsync();
		}
	}
}
