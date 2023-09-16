using AutoMapper;
using Data;
using Data.Classes;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
using Data.Repositories;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LuminaPath.Controllers
{
	public class GamesController : BasicController<Game, GamesDto>
	{
		private readonly ILogger<GamesController> _logger;
		private readonly GameRepo _gameService;

		public GamesController(GameRepo service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_logger = logger;
			_includes = new List<string> { "PersonalGamings" };
			_gameService = service;
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<PaginatedResult<GamesDto>> Get([FromQuery] MediaFIlter filter)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			PaginatedList<Game> entities;
			if(userId != null)
			{
				entities = await _gameService.GetAll(filter, userId);
			}
			else
			{
				entities = await _gameService.GetAll(filter);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}
	}
}
