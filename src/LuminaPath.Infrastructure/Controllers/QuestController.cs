using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class QuestsController : ControllerBase
    {
        private readonly QuestService _service;
        private readonly UserManager<LuminaUser> _userManager;

        public QuestsController(QuestService service, UserManager<LuminaUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet("board")]
        public async Task<ActionResult<QuestBoardDto>> GetBoard()
        {
            var user = await GetCurrentUser();
            return user == null
                ? Unauthorized("Please Login")
                : Ok(await _service.GetBoardAsync(user.Id));
        }

        [HttpPut("board")]
        public async Task<ActionResult<QuestBoardDto>> SaveBoard(QuestBoardDto board)
        {
            var user = await GetCurrentUser();
            return user == null
                ? Unauthorized("Please Login")
                : Ok(await _service.SaveBoardAsync(user.Id, board));
        }

        [HttpGet("for-game/{myGameId:int}")]
        public async Task<ActionResult<List<QuestDto>>> GetForGame(int myGameId)
        {
            var user = await GetCurrentUser();
            return user == null
                ? Unauthorized("Please Login")
                : Ok(await _service.GetQuestsForMyGameAsync(user.Id, myGameId));
        }

        private async Task<LuminaUser?> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
        }
    }
}
