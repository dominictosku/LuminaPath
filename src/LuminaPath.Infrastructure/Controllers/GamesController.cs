using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.Third_Party;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers
{
    public class GamesController : MediaController<Game, GamesDto, GameService, MyGame>
    {
        private readonly GameNewsService _gameNewsService;

        public GamesController(GameService service, IObjectMapper mapper, GameNewsService gameNewsService) : base(service, mapper)
        {
            _gameNewsService = gameNewsService;
            Includes = new List<string>() { "Image", "MyGames" };
        }

        [HttpGet("{id:int}/news")]
        public async Task<ActionResult<IReadOnlyList<GameNewsItemDto>>> GetNews(int id, [FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
        {
            var result = await _gameNewsService.GetNewsAsync(id, refresh, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
    }
}
