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
    public class GamingSessionsController : AuthorizedControllerBase
    {
        private readonly GamingSessionService _service;

        public GamingSessionsController(GamingSessionService service, UserManager<LuminaUser> userManager)
            : base(userManager)
        {
            _service = service;
        }

        [HttpGet]
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
        public async Task<ActionResult<List<GamingSessionDto>>> Get(
            [FromQuery] int? myGameId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var user = await GetCurrentUserAsync();
            return user == null
                ? LoginRequired()
                : Ok(await _service.GetForUserAsync(user.Id, myGameId, from, to));
        }

        [HttpPost]
        public async Task<ActionResult<GamingSessionDto>> Post(GamingSessionDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.CreateAsync(user.Id, dto);
            return result.Match<ActionResult>(
                created => CreatedAtAction(nameof(Get), new { id = created.Id }, created),
                failed => BadRequest(failed));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<GamingSessionDto>> Put(int id, GamingSessionDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.UpdateAsync(user.Id, id, dto);
            return result.Match<ActionResult>(
                updated => Ok(updated),
                failed => BadRequest(failed));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var result = await _service.DeleteAsync(user.Id, id);
            return result.Match<ActionResult>(
                _ => Ok(),
                failed => NotFound(failed));
        }

        [HttpGet("forecast/{myGameId:int}")]
        [EnableRateLimiting(RateLimitPolicies.BroadReads)]
        public async Task<ActionResult<GameForecastDto>> Forecast(int myGameId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return LoginRequired();

            var forecast = await _service.GetForecastAsync(user.Id, myGameId);
            return forecast == null ? NotFound("Game not found in your library.") : Ok(forecast);
        }
    }
}
