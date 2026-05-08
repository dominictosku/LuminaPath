using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.Third_Party;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    public class GamesController : GenericController<Game, GamesDto>
    {
        private new readonly GameService _service;
        private readonly GameNewsService _gameNewsService;

        public GamesController(GameService service, IObjectMapper mapper, GameNewsService gameNewsService) : base(service, mapper)
        {
            _service = service;
            _gameNewsService = gameNewsService;
            Includes = new List<string>() { "Image", "MyGames" };
        }

        [HttpGet]
        public override async Task<ActionResult<PaginatedResult<GamesDto>>> Get([FromQuery] MediaFilter mediaFilter)
        {
            string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.GetAndMapEntities<GamesDto>(mediaFilter, Includes, userId);
            return Ok(new PaginatedResult<GamesDto>(result));
        }

        [HttpGet("{id:int}/news")]
        public async Task<ActionResult<IReadOnlyList<GameNewsItemDto>>> GetNews(int id, [FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
        {
            var result = await _gameNewsService.GetNewsAsync(id, refresh, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
    }
}
