using AutoMapper;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    public class GamesController : GenericController<Game, GamesDto>
    {
        private new readonly GameService _service;
        private readonly ILogger<GamesController> _logger;

        public GamesController(GameService service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
        {
            _service = service;
            _logger = logger;
            Includes = new List<string>() { "Image" };
        }

        [HttpGet]
        public override async Task<ActionResult<PaginatedResult<GamesDto>>> Get([FromQuery] MediaFilter mediaFilter)
        {
            string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.GetAndMapEntities<GamesDto>(mediaFilter, Includes, userId);
            return Ok(new PaginatedResult<GamesDto>(result));
        }
    }
}
