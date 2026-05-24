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
            var user = await GetCurrentUserAsync();
            return user == null ? LoginRequired() : Ok(await _service.GetForUserAsync(user.Id));
        }

        [HttpGet("search")]
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
        public async Task<ActionResult<List<FriendUserDto>>> Search([FromQuery] string query)
        {
            var user = await GetCurrentUserAsync();
            return user == null ? LoginRequired() : Ok(await _service.SearchUsersAsync(user.Id, query));
        }

        public class SendRequestDto
        {
            public string AddresseeId { get; set; } = string.Empty;
        }

        [HttpPost("requests")]
        public async Task<ActionResult<FriendshipDto>> SendRequest([FromBody] SendRequestDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.SendRequestAsync(user.Id, dto.AddresseeId);
            return result.Match<ActionResult>(
                created => Ok(created),
                failed => BadRequest(failed));
        }

        [HttpPost("requests/{id:int}/accept")]
        public async Task<ActionResult<FriendshipDto>> Accept(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();
            var result = await _service.RespondAsync(user.Id, id, accept: true);
            return result.Match<ActionResult>(Ok, NotFound);
        }

        [HttpPost("requests/{id:int}/decline")]
        public async Task<ActionResult<FriendshipDto>> Decline(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();
            var result = await _service.RespondAsync(user.Id, id, accept: false);
            return result.Match<ActionResult>(Ok, NotFound);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();
            var result = await _service.RemoveAsync(user.Id, id);
            return result.Match<ActionResult>(_ => Ok(), NotFound);
        }
    }
}
