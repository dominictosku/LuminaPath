using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;

namespace LuminaPath.Infrastructure.Controllers
{
    public class SeriesController : MediaController<Series, SeriesDto, SeriesService, MySeries>
    {
        public SeriesController(SeriesService service, IObjectMapper mapper) : base(service, mapper)
        {
            Includes = new List<string>() { nameof(Series.Image), nameof(Series.MySeries) };
        }
    }
}
