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
    public class GamingSessionsController : ControllerBase
    {
        private readonly GamingSessionService _service;
        private readonly UserManager<LuminaUser> _userManager;

        public GamingSessionsController(GamingSessionService service, UserManager<LuminaUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<GamingSessionDto>>> Get(
            [FromQuery] int? myGameId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var user = await GetCurrentUser();
            return user == null
                ? Unauthorized("Please Login")
                : Ok(await _service.GetForUserAsync(user.Id, myGameId, from, to));
        }

        [HttpPost]
        public async Task<ActionResult<GamingSessionDto>> Post(GamingSessionDto dto)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");

            var result = await _service.CreateAsync(user.Id, dto);
            return result.Match<ActionResult>(
                created => CreatedAtAction(nameof(Get), new { id = created.Id }, created),
                failed => BadRequest(failed));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<GamingSessionDto>> Put(int id, GamingSessionDto dto)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");

            var result = await _service.UpdateAsync(user.Id, id, dto);
            return result.Match<ActionResult>(
                updated => Ok(updated),
                failed => BadRequest(failed));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");

            var result = await _service.DeleteAsync(user.Id, id);
            return result.Match<ActionResult>(
                _ => Ok(),
                failed => NotFound(failed));
        }

        [HttpGet("forecast/{myGameId:int}")]
        public async Task<ActionResult<GameForecastDto>> Forecast(int myGameId)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized("Please Login");

            var forecast = await _service.GetForecastAsync(user.Id, myGameId);
            return forecast == null ? NotFound("Game not found in your library.") : Ok(forecast);
        }

        private async Task<LuminaUser?> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
        }
    }
}
