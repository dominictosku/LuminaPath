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
    public class FriendsController : ControllerBase
    {
        private readonly FriendsService _service;
        private readonly UserManager<LuminaUser> _userManager;

        public FriendsController(FriendsService service, UserManager<LuminaUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<FriendshipDto>>> Get()
        {
            var user = await GetCurrentUser();
            return user == null ? Unauthorized("Please Login") : Ok(await _service.GetForUserAsync(user.Id));
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<FriendUserDto>>> Search([FromQuery] string query)
        {
            var user = await GetCurrentUser();
            return user == null ? Unauthorized("Please Login") : Ok(await _service.SearchUsersAsync(user.Id, query));
        }

        public class SendRequestDto
        {
            public string AddresseeId { get; set; } = string.Empty;
        }

        [HttpPost("requests")]
        public async Task<ActionResult<FriendshipDto>> SendRequest([FromBody] SendRequestDto dto)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");

            var result = await _service.SendRequestAsync(user.Id, dto.AddresseeId);
            return result.Match<ActionResult>(
                created => Ok(created),
                failed => BadRequest(failed));
        }

        [HttpPost("requests/{id:int}/accept")]
        public async Task<ActionResult<FriendshipDto>> Accept(int id)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");
            var result = await _service.RespondAsync(user.Id, id, accept: true);
            return result.Match<ActionResult>(Ok, NotFound);
        }

        [HttpPost("requests/{id:int}/decline")]
        public async Task<ActionResult<FriendshipDto>> Decline(int id)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");
            var result = await _service.RespondAsync(user.Id, id, accept: false);
            return result.Match<ActionResult>(Ok, NotFound);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");
            var result = await _service.RemoveAsync(user.Id, id);
            return result.Match<ActionResult>(_ => Ok(), NotFound);
        }

        private async Task<LuminaUser?> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
        }
    }
}
