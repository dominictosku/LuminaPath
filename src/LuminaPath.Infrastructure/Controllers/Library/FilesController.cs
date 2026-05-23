using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Identity;
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
        /// <summary>
        /// 3 MB hard cap on uploaded covers — matches DocumentService's
        /// MaxAllowedSize for IBrowserFile uploads. Anything larger gets
        /// rejected before we touch the storage backend.
        /// </summary>
        private const long MaxUploadBytes = 3 * 1024 * 1024;

        private readonly IStorageService Storage;
        private readonly AuditLogService? _auditLog;

        public FilesController(IStorageService azureStorage, AuditLogService? auditLog = null)
        {
            Storage = azureStorage;
            _auditLog = auditLog;
        }

        /// <summary>
        /// Uploads a single cover image. Gated by the CatalogEditors
        /// policy because the only legitimate caller is the catalog
        /// create/edit dialog. We additionally check that the file is
        /// non-empty, fits the size cap, and has an image/* content type
        /// — otherwise this endpoint would happily store arbitrary blobs
        /// on behalf of any logged-in editor.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
        public async Task<IActionResult> PostImage(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                await AuditFileAsync(
                    AuditActions.DocumentUploaded,
                    AuditOutcomes.Failure,
                    file?.FileName,
                    file?.ContentType,
                    "Empty upload.");
                return BadRequest("No file was uploaded.");
            }

            if (file.Length > MaxUploadBytes)
            {
                await AuditFileAsync(
                    AuditActions.DocumentUploaded,
                    AuditOutcomes.Failure,
                    file.FileName,
                    file.ContentType,
                    $"File exceeds the {MaxUploadBytes / 1024} KiB limit.");
                return new ObjectResult($"File is too large. Maximum allowed size is {MaxUploadBytes / 1024} KiB.")
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge,
                };
            }

            if (!IsAllowedImageContentType(file.ContentType))
            {
                await AuditFileAsync(
                    AuditActions.DocumentUploaded,
                    AuditOutcomes.Failure,
                    file.FileName,
                    file.ContentType,
                    "Unsupported content type.");
                return new ObjectResult("Only image uploads are allowed (png, jpg, webp, gif, bmp, svg).")
                {
                    StatusCode = StatusCodes.Status415UnsupportedMediaType,
                };
            }

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

            var storedName = result.Blob.Name ?? file.FileName;
            var contentType = result.Blob.ContentType ?? file.ContentType;

            await AuditFileAsync(
                AuditActions.DocumentUploaded,
                AuditOutcomes.Success,
                storedName,
                contentType);

            // Return enough metadata for callers to wire the file into a
            // freshly-created entity (e.g. Game.Image) without needing a
            // follow-up lookup. `Url` matches Document.Url so the mobile
            // app can render the cover immediately.
            return Ok(new
            {
                Message = "File uploaded successfully.",
                Name = file.FileName,
                StorageName = storedName,
                ContentType = contentType,
                Url = $"api/files/{storedName}"
            });
        }

        private static bool IsAllowedImageContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
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
