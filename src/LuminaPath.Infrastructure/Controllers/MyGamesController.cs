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
    public class MyGamesController : UserMediaController<MyGame, MyGameDto>
    {
        public MyGamesController(MyGameService service, UserManager<LuminaUser> userManager,
            IObjectMapper mapper) : base(service, userManager, mapper)
        {
            Includes = new List<string> { nameof(MyGame.Game), nameof(MyGame.MyGameInfo) };
        }

        [HttpGet("{id:int}/achievements")]
        public async Task<ActionResult<List<UserGameAchievementDto>>> GetAchievements(int id)
        {
            var (user, _) = await GetCurrentUserWithIdAsync();
            if (user is null)
            {
                return LoginRequired();
            }

            var achievements = await ((MyGameService)_service).GetEarnedAchievementsAsync(id, user.Id);
            if (achievements is null)
            {
                return NotFound();
            }

            return Ok(achievements);
        }

        protected override void PrepareForUpdate(MyGameDto viewModel)
        {
            viewModel.MyGameInfo = null;
        }
    }
}
