using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    public class GamesController : MediaController<Game, GamesDto, GameService, MyGame>
    {
        private readonly NewsAggregationService _newsAggregationService;

        public GamesController(GameService service, IObjectMapper mapper, NewsAggregationService newsAggregationService) : base(service, mapper)
        {
            _newsAggregationService = newsAggregationService;
            Includes = new List<string>() { nameof(Game.Image), nameof(Game.MyGames) };
        }

        public override async Task<ActionResult<GamesDto>> GetById(int? id)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var detailIncludes = Includes.Concat(new[] { nameof(Game.ParentGame), nameof(Game.Dlcs) });
            var result = await MediaService.GetByIdAndMap<GamesDto>(id, detailIncludes, userId);
            return Ok(result);
        }

        [HttpGet("{id:int}/news")]
        public async Task<ActionResult<IReadOnlyList<GameNewsItemDto>>> GetNews(int id, [FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
        {
            var result = await _newsAggregationService.GetGameNewsAsync(id, refresh, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("{id:int}/dlcs")]
        public async Task<ActionResult<IReadOnlyList<GamesNoIncludeDto>>> GetDlcs(int id, CancellationToken cancellationToken = default)
        {
            var dlcs = await ((GameService)_service).GetDlcsAsync(id, cancellationToken);
            return Ok(Mapper.Map<IEnumerable<GamesNoIncludeDto>>(dlcs).ToList());
        }
    }
}
