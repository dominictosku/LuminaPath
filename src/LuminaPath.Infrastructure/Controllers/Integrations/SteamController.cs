using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers;

[ApiController]
[Route("api/steam")]
[Authorize]
public sealed class SteamController : AuthorizedControllerBase
{
    private readonly MediaImportService _mediaImportService;

    public SteamController(MediaImportService mediaImportService, UserManager<LuminaUser> userManager)
        : base(userManager)
    {
        _mediaImportService = mediaImportService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        return Ok(new { configured = await _mediaImportService.IsSteamConfiguredAsync(cancellationToken) });
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] SteamIdentifierRequest request, CancellationToken cancellationToken)
    {
        if (!await _mediaImportService.IsSteamConfiguredAsync(cancellationToken))
        {
            return Problem("Steam Web API key is not configured on the server.", statusCode: 503);
        }
        if (string.IsNullOrWhiteSpace(request?.Identifier))
        {
            return BadRequest(new { message = "Identifier is required." });
        }

        var preview = await _mediaImportService.PreviewSteamLibraryAsync(request.Identifier, cancellationToken);
        if (preview is null)
        {
            return NotFound(new { message = "Could not resolve that Steam ID. Check the spelling or use the steamID64." });
        }

        return Ok(preview);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] SteamIdentifierRequest request, CancellationToken cancellationToken)
    {
        if (!await _mediaImportService.IsSteamConfiguredAsync(cancellationToken))
        {
            return Problem("Steam Web API key is not configured on the server.", statusCode: 503);
        }
        if (string.IsNullOrWhiteSpace(request?.Identifier))
        {
            return BadRequest(new { message = "Identifier is required." });
        }

        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _mediaImportService.ImportSteamLibraryAsync(user, request.Identifier, cancellationToken);
        if (string.IsNullOrEmpty(result.SteamId))
        {
            return NotFound(new { message = "Could not resolve that Steam ID." });
        }

        if (result.Total == 0)
        {
            return Ok(new
            {
                result,
                message = "No games returned. Make sure your Steam profile and game details are public.",
            });
        }

        return Ok(new { result });
    }
}

public sealed class SteamIdentifierRequest
{
    public string Identifier { get; set; } = string.Empty;
}
