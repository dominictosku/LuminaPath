using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    public class AnimesController : MediaController<Anime, AnimesDto, AnimeService, MyAnime>
    {
        public AnimesController(AnimeService service, IObjectMapper mapper) : base(service, mapper)
        {
            Includes = new List<string>() { nameof(Anime.Image), nameof(Anime.MyAnimes) };
        }

        public override async Task<ActionResult<AnimesDto>> GetById(int? id)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var detailIncludes = Includes.Concat(new[] { nameof(Anime.ParentAnime), nameof(Anime.Seasons) });
            var result = await MediaService.GetByIdAndMap<AnimesDto>(id, detailIncludes, userId);
            return Ok(result);
        }

        [HttpGet("{id:int}/seasons")]
        public async Task<ActionResult<IReadOnlyList<AnimesNoIncludeDto>>> GetSeasons(int id, CancellationToken cancellationToken = default)
        {
            var seasons = await MediaService.GetSeasonsAsync(id, cancellationToken);
            return Ok(Mapper.Map<IEnumerable<AnimesNoIncludeDto>>(seasons).ToList());
        }
    }
}
