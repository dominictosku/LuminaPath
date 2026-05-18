using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/browse")]
    [Authorize]
    public class BrowseController : ControllerBase
    {
        private readonly BrowseLibraryService _browseLibraryService;

        public BrowseController(BrowseLibraryService browseLibraryService)
        {
            _browseLibraryService = browseLibraryService;
        }

        [HttpGet("games/releases")]
        public async Task<ActionResult<IReadOnlyList<BrowseItemDto>>> GetGameReleases(CancellationToken cancellationToken)
        {
            return Ok(await _browseLibraryService.GetGameReleasesAsync(CurrentUserId(), cancellationToken));
        }

        [HttpGet("animes/releases")]
        public async Task<ActionResult<IReadOnlyList<BrowseItemDto>>> GetAnimeReleases(CancellationToken cancellationToken)
        {
            return Ok(await _browseLibraryService.GetAnimeReleasesAsync(CurrentUserId(), cancellationToken));
        }

        private string? CurrentUserId()
            => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
