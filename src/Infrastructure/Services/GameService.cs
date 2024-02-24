using Application.Common.Extensions;
using AutoMapper;
using Domain.Common.Entities;
using Domain.Common.Entities.Results;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
	public class GameService : GenericModelService<Game>
	{
		public GameService(LuminaPathDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
		{

		}

        public async Task<PaginatedList<Game>> GetAllPaginated(
            MediaFilter mediaFilter,
			string UserId,
			Expression<Func<Game, bool>> filter = null,
			IEnumerable<string> includes = null)
        {
            IQueryable<Game> entities = _entities.Include(g => g.Image).Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
            entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.ReleaseDate), includes);
            return await CreatePaginatedList(entities, mediaFilter.Paging);
        }

        public async Task<PaginatedResult<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes, string? userId)
		{
			Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

			PaginatedList<Game> entities = userId != null
				? await GetAllPaginated(mediaFilter, userId, filter)
				: await GetAllPaginated(mediaFilter, includes, filter);

			var entitiesDto = _mapper.Map<IEnumerable<Game>, IEnumerable<TDto>>(entities);
			return new PaginatedResult<TDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
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
