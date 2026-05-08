using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class QuestsController : AuthorizedControllerBase
    {
        private readonly QuestService _service;

        public QuestsController(QuestService service, UserManager<LuminaUser> userManager)
            : base(userManager)
        {
            _service = service;
        }

        [HttpGet("board")]
        public async Task<ActionResult<QuestBoardDto>> GetBoard()
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.GetBoardAsync(user.Id));
        }

        [HttpPut("board")]
        public async Task<ActionResult<QuestBoardDto>> SaveBoard(QuestBoardDto board)
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.SaveBoardAsync(user.Id, board));
        }

        [HttpGet("for-game/{myGameId:int}")]
        public async Task<ActionResult<List<QuestDto>>> GetForGame(int myGameId)
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.GetQuestsForMyGameAsync(user.Id, myGameId));
        }
    }
}
