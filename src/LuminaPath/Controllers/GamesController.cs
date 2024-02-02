using Application.Helper;
using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Dto.Gaming;
using Domain.Entities;
using Domain.Models.Gaming;
using Infrastructure.Interfaces.Repositories;
using Infrastructure.Services;
using LuminaPath.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Security.Claims;

namespace LuminaPath.Controllers
{
	public class GamesController : GenericController<Game, GamesDto>
	{
        protected readonly IGameRepository _repository;
        private readonly ILogger<GamesController> _logger;

		public GamesController(IGameRepository gameRepo, GameService service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_repository = gameRepo;
			_logger = logger;
            Includes = new List<string>() { "Image" };
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<ActionResult<PaginatedResult<GamesDto>>> Get([FromQuery] MediaFilter mediaFilter)
		{
			string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

			PaginatedList<Game> entities = userId != null
				? await _repository.GetAllPaginated(mediaFilter.Paging, userId, filter)
				: await _repository.GetAllPaginated(mediaFilter.Paging, filter, includes: Includes);

			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
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
