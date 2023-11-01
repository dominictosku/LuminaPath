using AutoMapper;
using Data;
using Data.Classes;
using Data.Interfaces;
using Data.Models.Dto.Gaming;
using Data.Models.Gaming;
using Data.Repositories;
using LuminaPath.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Security.Claims;

namespace LuminaPath.Controllers
{
	public class GamesController : MediaController<Game, GamesDto>
	{
		private readonly ILogger<GamesController> _logger;
		private readonly GameRepo _gameService;

		public GamesController(GameRepo service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_logger = logger;;
			_gameService = service;
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<PaginatedResult<GamesDto>> Get([FromQuery] MediaFilter mediaFilter)
		{
			string userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			PaginatedList<Game> entities;
			Expression<Func<Game, bool>>? filter = null;
			if(mediaFilter.SearchString != null)
			{
				filter = g => g.Name.Contains(mediaFilter.SearchString);
			}
			if(userId != null)
			{
				entities = await _gameService.GetAllPaginated(mediaFilter.Paging, userId, filter);
			}
			else
			{
				entities = await _gameService.GetAllPaginated(mediaFilter.Paging, filter);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}
	}
}
