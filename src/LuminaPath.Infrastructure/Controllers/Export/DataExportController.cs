using LuminaPath.Infrastructure.Controllers.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
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
    public const string ExportFileNameHeader = "X-LuminaPath-Export-Filename";

    private const string LibraryWorkbookFileName = "LuminaLibrary.xlsx";
    private const string LibraryWorkbookContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly UserDataExportService _exportService;
    private readonly ExcelService _excelService;

    public DataExportController(
        UserDataExportService exportService,
        ExcelService excelService,
        UserManager<LuminaUser> userManager)
        : base(userManager)
    {
        _exportService = exportService;
        _excelService = excelService;
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
        Response.Headers.TryAdd(ExportFileNameHeader, export.FileName);

        return File(export.Content, "application/json", export.FileName);
    }

    [HttpGet("library.xlsx")]
    public async Task<IActionResult> ExportLibraryWorkbook()
    {
        var (user, _) = await GetCurrentUserWithIdAsync();
        if (user is null)
        {
            return LoginRequired();
        }

        var workbook = await _excelService.ExportLibraryWorkbookAsync(user);
        Response.Headers.TryAdd(ExportFileNameHeader, LibraryWorkbookFileName);

        return File(workbook, LibraryWorkbookContentType, LibraryWorkbookFileName);
    }
}
