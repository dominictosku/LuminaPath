using Application.Common.Extensions;
using Application.Common.Interfaces.Repositories;
using AutoMapper;
using Domain.Common.Entities;
using Domain.Common.Entities.Results;
using Domain.Models;
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
		protected new readonly IGameRepository _repository;
		public GameService(IGameRepository repo, IMapper mapper) : base(repo, mapper)
		{
			_repository = repo;
		}

        public async Task<PaginatedList<Game>> GetEntities(MediaFilter mediaFilter, IEnumerable<string> includes, string? userId)
        {
            Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

            return userId != null
                ? await _repository.GetAllPaginated(mediaFilter.Paging, userId, filter)
                : await _repository.GetAllPaginated(mediaFilter.Paging, filter, includes: includes);
        }

        public async Task<PaginatedResult<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes, string? userId)
		{
			Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

			PaginatedList<Game> entities = userId != null
				? await _repository.GetAllPaginated(mediaFilter.Paging, userId, filter)
				: await _repository.GetAllPaginated(mediaFilter.Paging, filter, includes: includes);

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
