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
    public class DirectMessagesController : ControllerBase
    {
        private readonly DirectMessageService _service;
        private readonly UserManager<LuminaUser> _userManager;

        public DirectMessagesController(DirectMessageService service, UserManager<LuminaUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet("{otherUserId}")]
        public async Task<ActionResult<List<DirectMessageDto>>> GetConversation(
            string otherUserId,
            [FromQuery] DateTime? before,
            [FromQuery] int take = 100)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");

            var result = await _service.GetConversationAsync(user.Id, otherUserId, take, before);
            return result.Match<ActionResult>(Ok, BadRequest);
        }

        [HttpPost]
        public async Task<ActionResult<DirectMessageDto>> Send([FromBody] SendDirectMessageDto dto)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");
            var result = await _service.SendAsync(user.Id, dto.RecipientId, dto.Content);
            return result.Match<ActionResult>(Ok, BadRequest);
        }

        [HttpPost("{otherUserId}/read")]
        public async Task<ActionResult<int>> MarkRead(string otherUserId)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");
            return Ok(await _service.MarkConversationReadAsync(user.Id, otherUserId));
        }

        private async Task<LuminaUser?> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
        }
    }
}
