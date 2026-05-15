using LuminaPath.Core.Dtos;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MyAnimesController : UserMediaController<MyAnime, MyAnimeDto>
    {
        public MyAnimesController(MyAnimeService service, UserManager<LuminaUser> userManager, IObjectMapper mapper)
            : base(service, userManager, mapper)
        {
            Includes = new List<string> { nameof(MyAnime.Anime) };
        }
    }
}
