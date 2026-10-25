using System.Security.Claims;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Controllers;

[ApiController]
[Route("api/integrations/google-drive")]
[Authorize(Roles = "Administrator")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GoogleDriveController(
    GoogleDriveConnectionService connection, IAntiforgery antiforgery, ILogger<GoogleDriveController> logger) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest("Reload the Drive settings page and try again."); }
        try { return Redirect(connection.BuildConnectUrl(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "")); }
        catch (InvalidOperationException) { return Redirect("/Admin/GoogleDrive?drive=error"); }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state,
        [FromQuery] string? error, CancellationToken ct)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        try
        {
            await connection.CompleteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", state, code, error, ct);
            return Redirect("/Admin/GoogleDrive?drive=connected");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Neither exception messages nor OAuth response bodies belong in logs or redirects.
            logger.LogWarning("Google Drive connection failed ({ErrorType}).", ex.GetType().Name);
            return Redirect("/Admin/GoogleDrive?drive=error");
        }
    }
}
