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
			_includes = new List<string> { "MyGames" };
			_gameService = service;
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<PaginatedResult<GamesDto>> Get([FromQuery] MediaFIlter filter)
		{
			string userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			PaginatedList<Game> entities;
			if(userId != null)
			{
				entities = await _gameService.GetAllPaginated(filter, userId);
			}
			else
			{
				entities = await _gameService.GetAllPaginated(filter);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		[HttpGet("All/{count}")]
		[AllowAnonymous]
		public override async Task<IEnumerable<GamesDto>> GetAll(int? count)
		{
			var entities = await _gameService.GetAll(count, _includes);
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return entitiesDto;
		}

		[HttpGet("{id}")]
		[AllowAnonymous]
		public override async Task<ActionResult<GamesDto>> GetById(int? id)
		{
			if (id == null)
				return NotFound();
			var entity = await _gameService.GetById(id, _includes);
			var entitiesDto = Mapper.Map<Game, GamesDto>(entity);
			if (entity == null)
				return NotFound();
			return Ok(entitiesDto);
		}
	}
}
