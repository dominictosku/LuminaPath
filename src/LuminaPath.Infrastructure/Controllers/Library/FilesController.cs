using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService Storage;
        private readonly AuditLogService? _auditLog;

        public FilesController(IStorageService azureStorage, AuditLogService? auditLog = null)
        {
            Storage = azureStorage;
            _auditLog = auditLog;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PostImage(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            var result = await Storage.UploadAsync(stream, file.FileName, file.ContentType);
            if (result.Error)
            {
                await AuditFileAsync(
                    AuditActions.DocumentUploaded,
                    AuditOutcomes.Failure,
                    file.FileName,
                    file.ContentType,
                    result.Status);
                return BadRequest(result.Status);
            }

            await AuditFileAsync(
                AuditActions.DocumentUploaded,
                AuditOutcomes.Success,
                result.Blob.Name ?? file.FileName,
                result.Blob.ContentType ?? file.ContentType);
            return Ok(new { Message = "File uploaded successfully." });
        }

        [HttpGet("{url}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(string url)
        {
            var result = await Storage.DownloadAsync(url);
            if (result is null || result.Content is null)
            {
                if (ShouldAuditDownload(null))
                {
                    await AuditFileAsync(
                        AuditActions.DocumentDownloaded,
                        AuditOutcomes.Failure,
                        url,
                        errorMessage: "File was not found.");
                }

                return File("/images/placeholder.png", "image/png");
            }

            if (ShouldAuditDownload(result.ContentType))
            {
                await AuditFileAsync(
                    AuditActions.DocumentDownloaded,
                    AuditOutcomes.Success,
                    url,
                    result.ContentType);
            }

            return new FileStreamResult(result.Content, result.ContentType ?? "image/png");
        }

        private bool ShouldAuditDownload(string? contentType)
        {
            if (Request.Query.ContainsKey("audit") || Request.Query.ContainsKey("download"))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(contentType)
                && !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private Task AuditFileAsync(
            string action,
            string outcome,
            string? fileName,
            string? contentType = null,
            string? errorMessage = null)
        {
            if (_auditLog is null)
            {
                return Task.CompletedTask;
            }

            return _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Document,
                Action = action,
                Outcome = outcome,
                TargetType = "File",
                TargetId = fileName,
                TargetName = fileName,
                Metadata = new
                {
                    source = "FilesController",
                    contentType
                },
                ErrorMessage = errorMessage
            });
        }
    }
}
