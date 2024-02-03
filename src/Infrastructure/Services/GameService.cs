using Application.Helper;
using AutoMapper;
using Domain.Dto.Gaming;
using Domain.Entities;
using Domain.Models.Gaming;
using Infrastructure.Interfaces.Repositories;
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

		public async Task<Result<PaginatedResult<TDto>, FailedResult>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes, string? userId)
		{
			Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

			PaginatedList<Game> entities = userId != null
				? await _repository.GetAllPaginated(mediaFilter.Paging, userId, filter)
				: await _repository.GetAllPaginated(mediaFilter.Paging, filter, includes: includes);

			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<TDto>>(entities);
			return new PaginatedResult<TDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		private Expression<Func<Game, bool>> GetFilterExpression(MediaFilter mediaFilter, string? userId)
		{
			Expression<Func<Game, bool>> filter = g => true;

			if (mediaFilter.SearchString != null)
			{
				filter = g => g.Name.Contains(mediaFilter.SearchString);
			}

			if (mediaFilter.MyMedia && userId != null)
			{
				filter = filter.And(g => g.MyGames != null && g.MyGames.Any(m => m.LuminaUserId == userId));
			}

			return filter;
		}
	}
}
