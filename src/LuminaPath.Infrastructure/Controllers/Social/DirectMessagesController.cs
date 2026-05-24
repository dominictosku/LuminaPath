using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DirectMessagesController : AuthorizedControllerBase
    {
        private readonly DirectMessageService _service;

        public DirectMessagesController(DirectMessageService service, UserManager<LuminaUser> userManager)
            : base(userManager)
        {
            _service = service;
        }

        [HttpGet("{otherUserId}")]
        [EnableRateLimiting(RateLimitPolicies.DirectMessages)]
        public async Task<ActionResult<List<DirectMessageDto>>> GetConversation(
            string otherUserId,
            [FromQuery] DateTime? before,
            [FromQuery] int take = 100)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.GetConversationAsync(user.Id, otherUserId, take, before);
            return result.Match<ActionResult>(Ok, BadRequest);
        }

        [HttpPost]
        [EnableRateLimiting(RateLimitPolicies.DirectMessages)]
        [RequestSizeLimit(DirectMessageLimits.MaxRequestBytes)]
        public async Task<ActionResult<DirectMessageDto>> Send([FromBody] SendDirectMessageDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();
            var result = await _service.SendAsync(user.Id, dto.RecipientId, dto.Content);
            return result.Match<ActionResult>(Ok, BadRequest);
        }

        [HttpPost("{otherUserId}/read")]
        [EnableRateLimiting(RateLimitPolicies.DirectMessages)]
        public async Task<ActionResult<int>> MarkRead(string otherUserId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();
            return Ok(await _service.MarkConversationReadAsync(user.Id, otherUserId));
        }
    }
}
