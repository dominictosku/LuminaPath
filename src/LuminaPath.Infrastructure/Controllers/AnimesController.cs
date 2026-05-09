using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;

namespace LuminaPath.Infrastructure.Controllers
{
    public class AnimesController : MediaController<Anime, AnimesDto, AnimeService, MyAnime>
    {
        public AnimesController(AnimeService service, IObjectMapper mapper) : base(service, mapper)
        {
            Includes = new List<string>() { nameof(Anime.Image), nameof(Anime.MyAnimes) };
        }
    }
}
