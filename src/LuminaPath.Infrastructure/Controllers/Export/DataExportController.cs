using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers;

[ApiController]
[Authorize]
[Route("api/export")]
public sealed class DataExportController : AuthorizedControllerBase
{
    private readonly UserDataExportService _exportService;

    public DataExportController(UserDataExportService exportService, UserManager<LuminaUser> userManager)
        : base(userManager)
    {
        _exportService = exportService;
    }

    [HttpGet("json")]
    public async Task<IActionResult> ExportJson(CancellationToken cancellationToken)
    {
        var (_, userId) = await GetCurrentUserWithIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return LoginRequired();
        }

        var export = await _exportService.ExportAsync(userId, cancellationToken);
        Response.Headers.TryAdd("X-LuminaPath-Export-Filename", export.FileName);

        return File(export.Content, "application/json", export.FileName);
    }
}
