using AutoMapper;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Infrastructure.Services
{
	public class MyGameService : GenericModelService<MyGame>
	{
		public MyGameService(LuminaPathDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
		{
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

		public async Task<Result<MyGame, FailedResult>> PostAsync(MyGame viewModel, LuminaUser? user)
		{
			if (user == null)
				return new FailedResult("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
			{
				return new FailedResult("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;

			return await base.PostAsync(viewModel);
		}

		public async Task<Result<MyGame, FailedResult>> PutAsync(MyGame viewModel, LuminaUser? user)
		{
			if (user == null)
				return new FailedResult("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
			{
				return new FailedResult("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PutAsync(viewModel);
		}

		protected bool IsMediaAlreadyAdded(int id, int myId, string userId)
		{
			var entities = _entities.AsNoTracking().ToList();
			return entities.Any(e => e.MediaId == id && e.Id != myId && e.LuminaUserId == userId);
		}

	}
}
