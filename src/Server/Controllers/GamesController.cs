using Application.Common.Features.Gaming.Dto;
using Application.Common.Interfaces.Repositories;
using AutoMapper;
using Domain.Common.Entities;
using Domain.Common.Interfaces;
using Domain.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Controllers.Base;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Server.Controllers
{
	public class GamesController : GenericController<Game, GamesDto>
	{
		private readonly new GameService _service;
		private readonly ILogger<GamesController> _logger;

		public GamesController(IGameRepository gameRepo, GameService service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_service = service;
			_logger = logger;
			Includes = new List<string>() { "Image" };
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<ActionResult<PaginatedResult<GamesDto>>> Get([FromQuery] MediaFilter mediaFilter)
		{
			string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var result = await _service.GetAndMapEntities<GamesDto>(mediaFilter, Includes, userId);
			return result.Match<ActionResult<PaginatedResult<GamesDto>>>(
				m => Ok(m),
				f => BadRequest(f));
		}
	}
}
