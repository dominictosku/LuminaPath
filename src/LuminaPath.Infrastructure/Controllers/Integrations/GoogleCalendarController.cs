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
    private readonly GoogleCalendarSettingsResolver _resolver;
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarController> _logger;

    public GoogleCalendarController(
        GoogleCalendarConnectionService connection,
        GoogleCalendarSyncService sync,
        GoogleCalendarSettingsResolver resolver,
        IOptions<GoogleCalendarOptions> options,
        UserManager<LuminaUser> userManager,
        ILogger<GoogleCalendarController> logger)
        : base(userManager)
    {
        _connection = connection;
        _sync = sync;
        _resolver = resolver;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("connect")]
    public async Task<IActionResult> Connect([FromQuery] string? returnPath, CancellationToken cancellationToken)
    {
        var safeReturn = ResolveReturnPath(returnPath);

        var settings = await _resolver.GetAsync(cancellationToken);
        if (!settings.IsConfigured)
        {
            return Redirect(BuildReturn(safeReturn, "unavailable"));
        }

        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Redirect(BuildReturn(safeReturn, "error"));
        }

        var url = await _connection.BuildConnectUrlAsync(userId, safeReturn, ResolveRedirectUri(), cancellationToken);
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        // The return path is signed into the state, so it survives the round
        // trip (and is honoured even on consent-denied callbacks).
        var returnPath = ResolveReturnPath(_connection.PeekReturnPath(state));
        var userId = CurrentUserId;

        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(userId))
        {
            return Redirect(BuildReturn(returnPath, "error"));
        }

        try
        {
            await _connection.CompleteConnectionAsync(userId, code, state, ResolveRedirectUri(), cancellationToken);
            return Redirect(BuildReturn(returnPath, "connected"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Calendar connection failed.");
            return Redirect(BuildReturn(returnPath, "error"));
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

        var settings = await _resolver.GetAsync(cancellationToken);
        var status = await _connection.GetStatusAsync(userId, cancellationToken);
        return Ok(new
        {
            configured = settings.IsConfigured,
            connected = status.Connected,
            email = status.Email,
            lastSyncedAt = status.LastSyncedAt,
        });
    }

    [HttpPost("sync")]
    [EnableRateLimiting(RateLimitPolicies.Imports)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var settings = await _resolver.GetAsync(cancellationToken);
        if (!settings.IsConfigured)
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

    // Only allow same-origin relative return paths (defends against open
    // redirects via a crafted ?returnPath). Falls back to the SPA settings page.
    private string ResolveReturnPath(string? candidate)
        => !string.IsNullOrWhiteSpace(candidate) && Url.IsLocalUrl(candidate)
            ? candidate
            : _options.SettingsReturnPath;

    // Relative redirect so the browser resolves it against the current origin
    // (the SPA frontend that proxies /api, or the Blazor app), not a fixed host.
    private static string BuildReturn(string returnPath, string status)
        => $"{returnPath}?google={status}";
}
