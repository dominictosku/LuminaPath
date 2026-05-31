using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Controllers;

/// <summary>
/// Google Calendar account link + manual one-way sync of the user's library
/// releases and dated quests. connect/callback are top-level browser
/// navigations (Lax cookie carries the session); status/sync/disconnect are
/// SPA XHR calls.
/// </summary>
[ApiController]
[Route("api/integrations/google")]
[Authorize]
public sealed class GoogleCalendarController : AuthorizedControllerBase
{
    private readonly GoogleCalendarConnectionService _connection;
    private readonly GoogleCalendarSyncService _sync;
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarController> _logger;

    public GoogleCalendarController(
        GoogleCalendarConnectionService connection,
        GoogleCalendarSyncService sync,
        IOptions<GoogleCalendarOptions> options,
        UserManager<LuminaUser> userManager,
        ILogger<GoogleCalendarController> logger)
        : base(userManager)
    {
        _connection = connection;
        _sync = sync;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("connect")]
    public IActionResult Connect()
    {
        if (!_options.IsConfigured)
        {
            return SettingsRedirect("unavailable");
        }

        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return SettingsRedirect("error");
        }

        var url = _connection.BuildConnectUrl(userId, ResolveRedirectUri());
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(userId))
        {
            return SettingsRedirect("error");
        }

        try
        {
            await _connection.CompleteConnectionAsync(userId, code, state, ResolveRedirectUri(), cancellationToken);
            return SettingsRedirect("connected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Calendar connection failed.");
            return SettingsRedirect("error");
        }
    }

    [HttpGet("status")]
    [EnableRateLimiting(RateLimitPolicies.BroadReads)]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var status = await _connection.GetStatusAsync(userId, cancellationToken);
        return Ok(new
        {
            configured = _options.IsConfigured,
            connected = status.Connected,
            email = status.Email,
            lastSyncedAt = status.LastSyncedAt,
        });
    }

    [HttpPost("sync")]
    [EnableRateLimiting(RateLimitPolicies.Imports)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return Problem("Google Calendar is not configured on the server.", statusCode: 503);
        }

        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var status = await _connection.GetStatusAsync(userId, cancellationToken);
        if (!status.Connected)
        {
            return BadRequest(new { message = "Connect your Google account first." });
        }

        try
        {
            var result = await _sync.SyncAsync(userId, cancellationToken);
            return Ok(new
            {
                result.ReleaseEvents,
                result.QuestEvents,
                result.Deleted,
                message = $"Synced {result.ReleaseEvents} release(s) and {result.QuestEvents} quest(s).",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Calendar sync failed for the current user.");
            return Problem("Sync failed. Try reconnecting your Google account.", statusCode: 502);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await _connection.DisconnectAsync(userId, cancellationToken);
        return NoContent();
    }

    private string ResolveRedirectUri()
        => string.IsNullOrWhiteSpace(_options.RedirectUri)
            ? $"{Request.Scheme}://{Request.Host}/api/integrations/google/callback"
            : _options.RedirectUri;

    // Relative redirect so the browser resolves it against the SPA origin
    // (the frontend that proxies /api), not the backend.
    private IActionResult SettingsRedirect(string status)
        => Redirect($"{_options.SettingsReturnPath}?google={status}");
}
