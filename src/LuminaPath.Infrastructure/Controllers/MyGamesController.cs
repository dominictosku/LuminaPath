using AutoMapper;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace LuminaPath.Infrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MyGamesController : GenericMyController<MyGame, MyGameDto>
    {
        private readonly ILogger<GamesController> _logger;

        public MyGamesController(MyGameService service, UserManager<LuminaUser> userManager,
            ILogger<GamesController> logger, IMapper mapper) : base(service, userManager, mapper)
        {
            _logger = logger;
            Includes = new List<string> { "Game" };
        }
    }
}
