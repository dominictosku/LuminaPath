using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
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

        [HttpPost("{questId:int}/subtasks")]
        public async Task<ActionResult<QuestMutationResultDto>> AddSubtask(int questId, QuestSubtaskCreateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.AddSubtaskAsync(user.Id, questId, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => NotFound(failed));
        }

        [HttpPatch("{questId:int}/subtasks/{subtaskId:int}")]
        public async Task<ActionResult<QuestMutationResultDto>> UpdateSubtask(int questId, int subtaskId, QuestSubtaskUpdateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.UpdateSubtaskAsync(user.Id, questId, subtaskId, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => NotFound(failed));
        }

        [HttpDelete("{questId:int}/subtasks/{subtaskId:int}")]
        public async Task<IActionResult> DeleteSubtask(int questId, int subtaskId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.DeleteSubtaskAsync(user.Id, questId, subtaskId);
            return result.Match<IActionResult>(_ => NoContent(), f => NotFound(f));
        }

        // ===== Folders =================================================
        // Kept under /api/quests/folders rather than a sibling controller
        // so they share the same auth attribute + base policy as the
        // parent quest endpoints.

        [HttpGet("folders")]
        public async Task<ActionResult<List<QuestFolderDto>>> GetFolders()
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.GetFoldersAsync(user.Id));
        }

        [HttpPost("folders")]
        public async Task<ActionResult<QuestFolderDto>> CreateFolder(QuestFolderCreateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.CreateFolderAsync(user.Id, dto);
            return result.Match<ActionResult<QuestFolderDto>>(folder => Ok(folder), failed => BadRequest(failed));
        }

        [HttpPatch("folders/{folderId:int}")]
        public async Task<ActionResult<QuestFolderDto>> UpdateFolder(int folderId, QuestFolderUpdateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.UpdateFolderAsync(user.Id, folderId, dto);
            return result.Match<ActionResult<QuestFolderDto>>(folder => Ok(folder), failed => NotFound(failed));
        }

        [HttpDelete("folders/{folderId:int}")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.DeleteFolderAsync(user.Id, folderId);
            return result.Match<IActionResult>(_ => NoContent(), f => NotFound(f));
        }
    }
}
