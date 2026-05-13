using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    public class SeriesController : MediaController<Series, SeriesDto, SeriesService, MySeries>
    {
        public SeriesController(SeriesService service, IObjectMapper mapper) : base(service, mapper)
        {
            Includes = new List<string>() { nameof(Series.Image), nameof(Series.MySeries) };
        }

        public override async Task<ActionResult<SeriesDto>> GetById(int? id)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var detailIncludes = Includes.Concat(new[] { nameof(Series.ParentSeries), nameof(Series.Seasons) });
            var result = await MediaService.GetByIdAndMap<SeriesDto>(id, detailIncludes, userId);
            return Ok(result);
        }

        [HttpGet("{id:int}/seasons")]
        public async Task<ActionResult<IReadOnlyList<SeriesNoIncludeDto>>> GetSeasons(int id, CancellationToken cancellationToken = default)
        {
            var seasons = await MediaService.GetSeasonsAsync(id, cancellationToken);
            return Ok(Mapper.Map<IEnumerable<SeriesNoIncludeDto>>(seasons).ToList());
        }
    }
}
