using Data.Classes;
using Data.Models;
using Data.Models.Dto;
using Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public async Task<PaginatedList<MyGame>> GetAllPaginated(MediaFIlter filter, string UserId)
		{
			var myGames = _entities.Where(g => g.LuminaUserId == UserId).Include(g => g.Game);
			return await PaginatedList<MyGame>.CreateAsync(myGames, filter?.PageIndex ?? 1, 10);
		}
	}
}
