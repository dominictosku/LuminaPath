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
    public class FriendsController : AuthorizedControllerBase
    {
        private readonly FriendsService _service;

        public FriendsController(FriendsService service, UserManager<LuminaUser> userManager)
            : base(userManager)
        {
            _service = service;
        }

        [HttpGet]
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
        public async Task<ActionResult<List<FriendshipDto>>> Get()
        {
            var userId = CurrentUserId;
            return string.IsNullOrWhiteSpace(userId) ? LoginRequired() : Ok(await _service.GetForUserAsync(userId));
        }

        [HttpGet("search")]
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
        public async Task<ActionResult<List<FriendUserDto>>> Search([FromQuery] string query)
        {
            var userId = CurrentUserId;
            return string.IsNullOrWhiteSpace(userId) ? LoginRequired() : Ok(await _service.SearchUsersAsync(userId, query));
        }

        [HttpGet("{userId}/profile")]
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
        public async Task<ActionResult<FriendProfileDto>> Profile([FromRoute] string userId, [FromQuery] int limit = 6)
        {
            var currentUserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId)) return LoginRequired();

            var result = await _service.GetFriendProfileAsync(currentUserId, userId, limit);
            return result.Match<ActionResult>(Ok, failed => NotFound(failed));
        }

        public class SendRequestDto
        {
            public string AddresseeId { get; set; } = string.Empty;
        }

        [HttpPost("requests")]
        public async Task<ActionResult<FriendshipDto>> SendRequest([FromBody] SendRequestDto dto)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();

            var result = await _service.SendRequestAsync(userId, dto.AddresseeId);
            return result.Match<ActionResult>(
                created => Ok(created),
                failed => BadRequest(failed));
        }

        [HttpPost("requests/{id:int}/accept")]
        public async Task<ActionResult<FriendshipDto>> Accept(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();
            var result = await _service.RespondAsync(userId, id, accept: true);
            return result.Match<ActionResult>(Ok, NotFound);
        }

        [HttpPost("requests/{id:int}/decline")]
        public async Task<ActionResult<FriendshipDto>> Decline(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();
            var result = await _service.RespondAsync(userId, id, accept: false);
            return result.Match<ActionResult>(Ok, NotFound);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return LoginRequired();
            var result = await _service.RemoveAsync(userId, id);
            return result.Match<ActionResult>(_ => Ok(), NotFound);
        }
    }
}
