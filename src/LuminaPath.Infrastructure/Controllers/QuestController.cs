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

        [HttpPut("skills")]
        public async Task<ActionResult<QuestBoardDto>> SaveSkills(QuestBoardDto board)
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.SaveSkillsAsync(user.Id, board));
        }

        [HttpGet("for-game/{myGameId:int}")]
        public async Task<ActionResult<List<QuestDto>>> GetForGame(int myGameId)
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.GetQuestsForMyGameAsync(user.Id, myGameId));
        }

        [HttpPost]
        public async Task<ActionResult<QuestMutationResultDto>> Create(QuestCreateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.CreateAsync(user.Id, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => BadRequest(failed));
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<QuestMutationResultDto>> Update(int id, QuestUpdateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.UpdateAsync(user.Id, id, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => NotFound(failed));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.DeleteAsync(user.Id, id);
            return result.Match<IActionResult>(_ => NoContent(), f => NotFound(f));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(List<QuestReorderItemDto> items)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.ReorderAsync(user.Id, items);
            return result.Match<IActionResult>(_ => NoContent(), f => BadRequest(f));
        }
    }
}
