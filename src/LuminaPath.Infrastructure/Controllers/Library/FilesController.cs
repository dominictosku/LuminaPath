using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        private const int MaxUploadRequestBytes = (3 * 1024 * 1024) + (64 * 1024);

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
        /// non-empty, fits the size cap, and is one of the raster image
        /// formats we can identify by file signature. SVG is deliberately
        /// excluded because serving it from the app origin can become
        /// stored XSS.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
        [EnableRateLimiting(RateLimitPolicies.Uploads)]
        [RequestSizeLimit(MaxUploadRequestBytes)]
        public async Task<IActionResult> PostImage(IFormFile? file, CancellationToken cancellationToken = default)
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

            await using var sourceStream = file.OpenReadStream();
            await using var bufferedStream = new MemoryStream();
            await sourceStream.CopyToAsync(bufferedStream, cancellationToken);
            bufferedStream.Position = 0;

            var imageType = DetectAllowedRasterImage(bufferedStream);
            if (imageType is null)
            {
                await AuditFileAsync(
                    AuditActions.DocumentUploaded,
                    AuditOutcomes.Failure,
                    file.FileName,
                    file.ContentType,
                    "Unsupported or unrecognized image signature.");
                return new ObjectResult("Only raster image uploads are allowed (png, jpg, webp, gif, bmp).")
                {
                    StatusCode = StatusCodes.Status415UnsupportedMediaType,
                };
            }

            bufferedStream.Position = 0;
            var storageName = $"{Guid.NewGuid():N}{imageType.Extension}";
            var result = await Storage.UploadAsync(bufferedStream, storageName, imageType.ContentType);
            if (result.Error)
            {
                await AuditFileAsync(
                    AuditActions.DocumentUploaded,
                    AuditOutcomes.Failure,
                    file.FileName,
                    imageType.ContentType,
                    result.Status);
                return BadRequest(result.Status);
            }

            var storedName = result.Blob.Name ?? storageName;
            var contentType = result.Blob.ContentType ?? imageType.ContentType;

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

        private static RasterImageType? DetectAllowedRasterImage(Stream stream)
        {
            Span<byte> header = stackalloc byte[16];
            var read = stream.Read(header);
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            if (StartsWith(header, read, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            {
                return new RasterImageType("image/png", ".png");
            }

            if (StartsWith(header, read, [0xFF, 0xD8, 0xFF]))
            {
                return new RasterImageType("image/jpeg", ".jpg");
            }

            if (StartsWith(header, read, [0x47, 0x49, 0x46, 0x38, 0x37, 0x61])
                || StartsWith(header, read, [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]))
            {
                return new RasterImageType("image/gif", ".gif");
            }

            if (read >= 12
                && MatchesAt(header, read, 0, [0x52, 0x49, 0x46, 0x46])
                && MatchesAt(header, read, 8, [0x57, 0x45, 0x42, 0x50]))
            {
                return new RasterImageType("image/webp", ".webp");
            }

            if (StartsWith(header, read, [0x42, 0x4D]))
            {
                return new RasterImageType("image/bmp", ".bmp");
            }

            return null;
        }

        private static bool StartsWith(ReadOnlySpan<byte> value, int bytesRead, ReadOnlySpan<byte> expected)
        {
            return MatchesAt(value, bytesRead, 0, expected);
        }

        private static bool MatchesAt(ReadOnlySpan<byte> value, int bytesRead, int offset, ReadOnlySpan<byte> expected)
        {
            if (bytesRead < offset + expected.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (value[offset + i] != expected[i])
                {
                    return false;
                }
            }

            return true;
        }

        private sealed record RasterImageType(string ContentType, string Extension);

        // Inherits the class-level [Authorize] — no AllowAnonymous on
        // file downloads. Cover images are still served to any signed-in
        // user (it's a shared catalog), but anonymous callers can't
        // enumerate or scrape blob storage by guessing names.
        [HttpGet("{url}")]
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
