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
            var userId = CurrentUserId;
            return string.IsNullOrWhiteSpace(userId)
                ? LoginRequired()
                : Ok(await _service.GetBoardAsync(userId));
        }

        [HttpPut("skills")]
        public async Task<ActionResult<QuestBoardDto>> SaveSkills(QuestBoardDto board)
        {
            var userId = CurrentUserId;
            return string.IsNullOrWhiteSpace(userId)
                ? LoginRequired()
                : Ok(await _service.SaveSkillsAsync(userId, board));
        }

        [HttpGet("for-game/{myGameId:int}")]
        public async Task<ActionResult<List<QuestDto>>> GetForGame(int myGameId)
        {
            var userId = CurrentUserId;
            return string.IsNullOrWhiteSpace(userId)
                ? LoginRequired()
                : Ok(await _service.GetQuestsForMyGameAsync(userId, myGameId));
        }

        [HttpPost]
        public async Task<ActionResult<QuestMutationResultDto>> Create(QuestCreateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.CreateAsync(userId, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => BadRequest(failed));
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<QuestMutationResultDto>> Update(int id, QuestUpdateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.UpdateAsync(userId, id, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => NotFound(failed));
        }

        [HttpPost("{id:int}/occurrences")]
        public async Task<ActionResult<QuestMutationResultDto>> MaterializeOccurrence(int id, QuestOccurrenceCreateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.MaterializeOccurrenceAsync(userId, id, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => BadRequest(failed));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.DeleteAsync(userId, id);
            return result.Match<IActionResult>(_ => NoContent(), f => NotFound(f));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(List<QuestReorderItemDto> items)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.ReorderAsync(userId, items);
            return result.Match<IActionResult>(_ => NoContent(), f => BadRequest(f));
        }

        [HttpPost("{questId:int}/subtasks")]
        public async Task<ActionResult<QuestMutationResultDto>> AddSubtask(int questId, QuestSubtaskCreateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.AddSubtaskAsync(userId, questId, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => NotFound(failed));
        }

        [HttpPatch("{questId:int}/subtasks/{subtaskId:int}")]
        public async Task<ActionResult<QuestMutationResultDto>> UpdateSubtask(int questId, int subtaskId, QuestSubtaskUpdateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.UpdateSubtaskAsync(userId, questId, subtaskId, dto);
            return result.Match<ActionResult<QuestMutationResultDto>>(
                mutation => Ok(mutation),
                failed => NotFound(failed));
        }

        [HttpDelete("{questId:int}/subtasks/{subtaskId:int}")]
        public async Task<IActionResult> DeleteSubtask(int questId, int subtaskId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.DeleteSubtaskAsync(userId, questId, subtaskId);
            return result.Match<IActionResult>(_ => NoContent(), f => NotFound(f));
        }

        // ===== Folders =================================================
        // Kept under /api/quests/folders rather than a sibling controller
        // so they share the same auth attribute + base policy as the
        // parent quest endpoints.

        [HttpGet("folders")]
        public async Task<ActionResult<List<QuestFolderDto>>> GetFolders()
        {
            var userId = CurrentUserId;
            return string.IsNullOrWhiteSpace(userId)
                ? LoginRequired()
                : Ok(await _service.GetFoldersAsync(userId));
        }

        [HttpPost("folders")]
        public async Task<ActionResult<QuestFolderDto>> CreateFolder(QuestFolderCreateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.CreateFolderAsync(userId, dto);
            return result.Match<ActionResult<QuestFolderDto>>(folder => Ok(folder), failed => BadRequest(failed));
        }

        [HttpPatch("folders/{folderId:int}")]
        public async Task<ActionResult<QuestFolderDto>> UpdateFolder(int folderId, QuestFolderUpdateDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.UpdateFolderAsync(userId, folderId, dto);
            return result.Match<ActionResult<QuestFolderDto>>(folder => Ok(folder), failed => NotFound(failed));
        }

        [HttpDelete("folders/{folderId:int}")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.DeleteFolderAsync(userId, folderId);
            return result.Match<IActionResult>(_ => NoContent(), f => NotFound(f));
        }

        [HttpPut("folders/reorder")]
        public async Task<IActionResult> ReorderFolders(List<QuestFolderReorderItemDto> items)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.ReorderFoldersAsync(userId, items);
            return result.Match<IActionResult>(_ => NoContent(), f => BadRequest(f));
        }
    }
}
