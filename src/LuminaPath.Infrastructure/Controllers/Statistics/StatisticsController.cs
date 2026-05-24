using LuminaPath.Core.Dtos.Statistics;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LuminaPath.Infrastructure.Controllers.Statistics;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class StatisticsController : AuthorizedControllerBase
{
    private readonly StatisticsService _service;

    public StatisticsController(StatisticsService service, UserManager<LuminaUser> userManager)
        : base(userManager)
    {
        _service = service;
    }

    /// <summary>
    /// Returns the current user's PlayStation trophy counts. Tier names
    /// match what PSN itself displays — bronze, silver, gold, platinum.
    /// </summary>
    [HttpGet("psn-trophies")]
    [EnableRateLimiting(RateLimitPolicies.BroadReads)]
    public async Task<ActionResult<PsnTrophyTotalsDto>> GetPsnTrophies(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return LoginRequired();
        }

        return Ok(await _service.GetPsnTrophyTotalsAsync(userId, cancellationToken));
    }
}
