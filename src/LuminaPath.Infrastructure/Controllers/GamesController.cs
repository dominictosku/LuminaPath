using AutoMapper;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Features.Gaming.Dto;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Security.Claims;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Controllers.Base;

namespace LuminaPath.Infrastructure.Controllers
{
	public class GamesController : GenericController<Game, GamesDto>
	{
		private readonly new GameService _service;
		private readonly ILogger<GamesController> _logger;

		public GamesController(GameService service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_service = service;
			_logger = logger;
			Includes = new List<string>() { "Image" };
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<ActionResult<PaginatedList<GamesDto>>> Get([FromQuery] MediaFilter mediaFilter)
		{
			string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var result = await _service.GetAndMapEntities<GamesDto>(mediaFilter, Includes, userId);
			return Ok(result);
		}
	}
}
