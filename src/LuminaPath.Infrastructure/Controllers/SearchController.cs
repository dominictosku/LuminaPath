using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LuminaPath.Infrastructure.Controllers;

[ApiController]
[Authorize]
[Route("api/search")]
public sealed class SearchController : AuthorizedControllerBase
{
    private readonly GlobalSearchService _searchService;

    public SearchController(GlobalSearchService searchService, UserManager<LuminaUser> userManager)
        : base(userManager)
    {
        _searchService = searchService;
    }

    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.BroadReads)]
    public async Task<ActionResult<IReadOnlyList<GlobalSearchResultDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var (_, userId) = await GetCurrentUserWithIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return LoginRequired();
        }

        return Ok(await _searchService.SearchAsync(userId, q, limit, cancellationToken));
    }
}
