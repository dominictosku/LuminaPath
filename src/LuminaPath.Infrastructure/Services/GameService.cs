using AutoMapper;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Extensions;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LuminaPath.Infrastructure;

namespace LuminaPath.Infrastructure.Services
{
	public class GameService : GenericModelService<Game>
	{
		public GameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IMapper mapper) : base(dbContextFactory, mapper)
		{

		}

		public async Task<PaginatedList<Game>> GetAllPaginated(
			MediaFilter mediaFilter,
			string UserId,
			Expression<Func<Game, bool>> filter = null,
			IEnumerable<string> includes = null)
		{
			using var context = await GetDbContextAsync();
			IQueryable<Game> entities = GetEntities(context);
			entities = entities.Include(g => g.Image).Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
			entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.ReleaseDate), includes);
			return await CreatePaginatedList(entities, mediaFilter.Paging);
		}

		public async Task<PaginatedList<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes, string? userId)
		{
			Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

			PaginatedList<Game> entities = userId != null
				? await GetAllPaginated(mediaFilter, userId, filter)
				: await GetAllPaginated(mediaFilter, includes, filter);

			var entitiesDto = _mapper.Map<IEnumerable<Game>, IEnumerable<TDto>>(entities);            
            return CreatePaginatedList(entitiesDto, mediaFilter.Paging);
		}

		private static Expression<Func<Game, bool>> GetFilterExpression(MediaFilter mediaFilter, string? userId = null)
		{
			Expression<Func<Game, bool>> filter = g => true;

			if (mediaFilter.SearchString != null)
			{
				filter = g => g.Name.Contains(mediaFilter.SearchString);
			}

			if (mediaFilter.From != null)
			{
				filter = filter.And(g => g.ReleaseDate > mediaFilter.From);
			}

			if (mediaFilter.To != null)
			{
				filter = filter.And(g => g.ReleaseDate < mediaFilter.To);
			}

			if (mediaFilter.MyMedia && userId != null)
			{
				filter = filter.And(g => g.MyGames != null && g.MyGames.Any(m => m.LuminaUserId == userId));
			}

			return filter;
		}
	}
}
