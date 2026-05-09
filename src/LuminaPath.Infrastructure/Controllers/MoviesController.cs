using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;

namespace LuminaPath.Infrastructure.Controllers
{
    public class MoviesController : MediaController<Movie, MoviesDto, MovieService, MyMovie>
    {
        public MoviesController(MovieService service, IObjectMapper mapper) : base(service, mapper)
        {
            Includes = new List<string>() { nameof(Movie.Image), nameof(Movie.MyMovies) };
        }
    }
}
