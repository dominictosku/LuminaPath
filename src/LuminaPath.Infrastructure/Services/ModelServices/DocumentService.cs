using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Extensions;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class DocumentService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly IStorageService _storage;
        private readonly ILogger<DocumentService> _logger;
        private readonly AuditLogService? _auditLog;
        private const long MaxAllowedSize = 3145728;

        public DocumentService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            IStorageService storage,
            ILogger<DocumentService> logger,
            AuditLogService? auditLog = null)
        {
            _dbContextFactory = dbContextFactory;
            _storage = storage;
            _logger = logger;
            _auditLog = auditLog;
        }

        protected async Task<LuminaPathDbContext> GetDbContextAsync()
        {
            return await _dbContextFactory.CreateDbContextAsync();
        }

        public virtual async Task<PaginatedList<MediaDocument>> GetAllPaginated(
            Paging paging,
            Expression<Func<MediaDocument, bool>>? filter = null,
            Func<IQueryable<MediaDocument>, IOrderedQueryable<MediaDocument>>? orderBy = null,
            IEnumerable<string>? includes = null)
        {
            await using var context = await GetDbContextAsync();
            IQueryable<MediaDocument> entities = context.MediaDocuments;
            entities = PrepareEntity(entities, filter, orderBy, includes);
            return await CreatePaginatedList(entities, paging);
        }

        public async Task<Result<MediaDocument, FailedResult>> CreateDocument(IBrowserFile file, IMedia<MediaDocument>? media = null)
        {
            var displayName = GetDisplayFileName(file.Name);
            try
            {
                await using Stream fs = file.OpenReadStream(MaxAllowedSize);
                return await UploadDocumentStreamAsync(fs, displayName, file.ContentType, media);
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not Upload File. error: {ex}", ex);
                await AuditDocumentFileUploadAsync(
                    displayName,
                    contentType: file.ContentType,
                    outcome: AuditOutcomes.Failure,
                    errorMessage: ex.Message);
                return new FailedResult("Could not upload file");
            }
        }

        public async Task<Result<MediaDocument, FailedResult>> CreateDocument(Stream stream, string fileName, string? contentType, IMedia<MediaDocument>? media = null)
        {
            var displayName = GetDisplayFileName(fileName);
            try
            {
                return await UploadDocumentStreamAsync(stream, displayName, contentType, media);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not upload file from stream");
                await AuditDocumentFileUploadAsync(
                    displayName,
                    contentType: contentType,
                    outcome: AuditOutcomes.Failure,
                    errorMessage: ex.Message);
                return new FailedResult("Could not upload file");
            }
        }

        private async Task<Result<MediaDocument, FailedResult>> UploadDocumentStreamAsync(
            Stream stream,
            string displayName,
            string? contentType,
            IMedia<MediaDocument>? media)
        {
            if (media is not null)
            {
                await DeleteDocument(media.Image);
            }

            var storageName = CreateStorageFileName(displayName, contentType);
            var result = await _storage.UploadAsync(stream, storageName, contentType);
            if (result.Error)
            {
                _logger.LogError("Could not upload file, error: {Status}", result.Status);
                await AuditDocumentFileUploadAsync(
                    displayName,
                    storageName,
                    contentType,
                    AuditOutcomes.Failure,
                    result.Status);
                return new FailedResult("Could not upload file");
            }

            var document = new MediaDocument
            {
                Name = displayName,
                StorageName = result.Blob.Name,
                Description = string.Empty,
                Path = string.Empty,
                ContentType = result.Blob.ContentType,
                DocumentType = InferDocumentType(displayName, result.Blob.ContentType)
            };
            await AuditDocumentFileUploadAsync(
                document.Name,
                document.StorageName,
                document.ContentType,
                AuditOutcomes.Success,
                documentType: document.DocumentType);
            return document;
        }

        public async Task<MediaDocument> CreateMediaDocument(MediaDocument document)
        {
            ValidateDocument(document);

            await using var context = await GetDbContextAsync();
            context.MediaDocuments.Add(document);
            await context.SaveChangesAsync();
            await AuditDocumentAsync(AuditActions.DocumentCreated, document);
            return document;
        }

        public async Task<MediaDocument> UpdateMediaDocument(MediaDocument document)
        {
            ValidateDocument(document);

            await using var context = await GetDbContextAsync();
            var existingDocument = await context.MediaDocuments.FirstOrDefaultAsync(d => d.Id == document.Id)
                ?? throw new KeyNotFoundException("Document not found");

            var oldName = existingDocument.Name;
            var oldStorageName = existingDocument.StorageName;
            var oldDescription = existingDocument.Description;
            var oldPath = existingDocument.Path;
            var oldContentType = existingDocument.ContentType;
            var oldDocumentType = existingDocument.DocumentType;
            var oldMediaId = existingDocument.MediaId;

            existingDocument.Name = document.Name;
            existingDocument.StorageName = string.IsNullOrWhiteSpace(document.StorageName)
                ? existingDocument.StorageName
                : document.StorageName;
            existingDocument.Description = document.Description;
            existingDocument.Path = document.Path;
            existingDocument.ContentType = document.ContentType;
            existingDocument.DocumentType = document.DocumentType;
            existingDocument.MediaId = document.MediaId;

            await context.SaveChangesAsync();
            await AuditDocumentAsync(
                AuditActions.DocumentUpdated,
                existingDocument,
                changes: AuditLogService.Changes(
                    ("Name", oldName, existingDocument.Name),
                    ("StorageName", oldStorageName, existingDocument.StorageName),
                    ("Description", oldDescription, existingDocument.Description),
                    ("Path", oldPath, existingDocument.Path),
                    ("ContentType", oldContentType, existingDocument.ContentType),
                    ("DocumentType", oldDocumentType, existingDocument.DocumentType),
                    ("MediaId", oldMediaId, existingDocument.MediaId)));
            return existingDocument;
        }

        public async Task DeleteDocument(MediaDocument? document)
        {
            if (document is null)
            {
                return;
            }

            try
            {
                await using var context = await GetDbContextAsync();
                await DeleteDocument(document, context);
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not delete file. error: {ex}", ex);
                await AuditDocumentAsync(
                    AuditActions.DocumentDeleted,
                    document,
                    AuditOutcomes.Failure,
                    errorMessage: ex.Message);
            }
        }

        public async Task DeleteMediaDocument(IMedia<MediaDocument>? entity, LuminaPathDbContext? _context = null)
        {
            if (entity is null)
            {
                return;
            }

            var ownsContext = _context == null;
            var context = _context ?? await GetDbContextAsync();
            try
            {
                if (entity.Image is null)
                    return;
                var image = entity.Image;
                entity.Image = null;
                context.Update(entity);
                await context.SaveChangesAsync();
                await DeleteDocument(image, context);
            }
            finally
            {
                if (ownsContext)
                {
                    await context.DisposeAsync();
                }
            }
        }

        private async Task DeleteDocument(MediaDocument document, LuminaPathDbContext context)
        {
            var existingDocument = await context.MediaDocuments.SingleOrDefaultAsync(d => d.Id == document.Id);
            if (existingDocument is null)
            {
                _logger.LogError("Could not find file, document: {0}", document.Name);
                await AuditDocumentAsync(
                    AuditActions.DocumentDeleted,
                    document,
                    AuditOutcomes.Failure,
                    "Document not found.");
                return;
            }

            var storageDeleteStatus = await DeleteStoredFileIfUnreferenced(context, existingDocument);

            if (existingDocument.MediaId != null)
            {
                var media = await context.Set<Media>()
                    .FirstOrDefaultAsync(item => item.Id == existingDocument.MediaId);
                if (media != null)
                {
                    media.Image = null;
                    context.Update(media);
                }
            }

            context.Documents.Remove(existingDocument);
            await context.SaveChangesAsync();
            await AuditDocumentAsync(
                AuditActions.DocumentDeleted,
                existingDocument,
                metadata: new
                {
                    source = "DocumentService",
                    storageDeleteStatus
                });
        }

        private async Task<string> DeleteStoredFileIfUnreferenced(LuminaPathDbContext context, MediaDocument document)
        {
            var storageName = GetStorageName(document);
            if (string.IsNullOrWhiteSpace(storageName))
            {
                return "NoStorageName";
            }

            var isReferenced = await context.Documents
                .AsNoTracking()
                .AnyAsync(item => item.Id != document.Id
                    && (item.StorageName == storageName || item.StorageName == null && item.Name == storageName));

            if (isReferenced)
            {
                return "StillReferenced";
            }

            var result = await _storage.DeleteAsync(storageName);
            if (result.Error)
            {
                _logger.LogWarning("Could not delete stored file {FileName}, removing database document anyway. Error: {Status}", storageName, result.Status);
                return "StorageDeleteFailed";
            }

            return "Deleted";
        }

        private Task AuditDocumentFileUploadAsync(
            string? displayName,
            string? storageName = null,
            string? contentType = null,
            string outcome = AuditOutcomes.Success,
            string? errorMessage = null,
            DocumentType? documentType = null)
        {
            var resolvedDocumentType = documentType ?? InferDocumentType(displayName ?? string.Empty, contentType);
            return AuditDocumentAsync(
                AuditActions.DocumentUploaded,
                new MediaDocument
                {
                    Name = displayName,
                    StorageName = storageName,
                    ContentType = contentType,
                    DocumentType = resolvedDocumentType
                },
                outcome,
                errorMessage,
                metadata: new
                {
                    source = "DocumentService",
                    contentType,
                    documentType = resolvedDocumentType.ToString(),
                    storageName
                });
        }

        private async Task AuditDocumentAsync(
            string action,
            MediaDocument document,
            string outcome = AuditOutcomes.Success,
            string? errorMessage = null,
            object? changes = null,
            object? metadata = null)
        {
            if (_auditLog is null)
            {
                return;
            }

            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Document,
                Action = action,
                Outcome = outcome,
                TargetType = nameof(MediaDocument),
                TargetId = document.Id > 0 ? document.Id.ToString() : document.StorageName ?? document.Name,
                TargetName = document.Name ?? document.StorageName ?? document.Path,
                Changes = changes,
                Metadata = metadata ?? new
                {
                    source = "DocumentService",
                    documentType = document.DocumentType.ToString(),
                    contentType = document.ContentType,
                    storageName = document.StorageName,
                    mediaId = document.MediaId
                },
                ErrorMessage = errorMessage
            });
        }

        private static string GetStorageName(MediaDocument document)
        {
            return document.StorageName ?? document.Name ?? string.Empty;
        }

        private static string GetDisplayFileName(string fileName)
        {
            var displayName = Path.GetFileName(fileName);
            return string.IsNullOrWhiteSpace(displayName)
                ? "upload"
                : displayName.Trim();
        }

        private static string CreateStorageFileName(string fileName, string? contentType)
        {
            var displayName = GetDisplayFileName(fileName);
            var extension = Path.GetExtension(displayName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = GetExtensionFromContentType(contentType);
            }

            return $"{Guid.NewGuid():N}{extension}";
        }

        private static void ValidateDocument(MediaDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.Name)
                && string.IsNullOrWhiteSpace(document.StorageName)
                && string.IsNullOrWhiteSpace(document.Path))
            {
                throw new InvalidOperationException("Document needs a file name or path.");
            }
        }

        private static string GetExtensionFromContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return string.Empty;
            }

            return contentType.ToLowerInvariant() switch
            {
                "image/apng" => ".apng",
                "image/avif" => ".avif",
                "image/bmp" => ".bmp",
                "image/gif" => ".gif",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/svg+xml" => ".svg",
                "image/webp" => ".webp",
                "application/pdf" => ".pdf",
                "text/plain" => ".txt",
                _ => string.Empty
            };
        }

        private static DocumentType InferDocumentType(string fileName, string? contentType)
        {
            if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            {
                return DocumentType.Image;
            }

            if (contentType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true)
            {
                return DocumentType.PDF;
            }

            var extension = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
            return extension switch
            {
                "PNG" or "JPG" or "JPEG" or "WEBP" or "GIF" or "BMP" or "SVG" => DocumentType.Image,
                "PDF" => DocumentType.PDF,
                "XLS" or "XLSX" or "CSV" => DocumentType.Excel,
                "DOC" or "DOCX" or "ODT" or "TXT" or "MD" => DocumentType.Document,
                _ => DocumentType.Others
            };
        }

        protected virtual async Task<PaginatedList<MediaDocument>> CreatePaginatedList(IQueryable<MediaDocument> entities, Paging paging)
        {
            int pageIndex = paging.PageIndex;
            int pageSize = paging.EffectiveCount;
            return await entities.ToPaginatedListAsync(pageIndex, pageSize);
        }

        protected virtual IQueryable<MediaDocument> PrepareEntity(
            IQueryable<MediaDocument> entities,
            Expression<Func<MediaDocument, bool>>? filter = null,
            Func<IQueryable<MediaDocument>, IOrderedQueryable<MediaDocument>>? orderBy = null,
            IEnumerable<string>? includes = null
            )
        {
            if (filter != null)
            {
                entities = entities.Where(filter);
            }
            if (includes != null)
            {
                entities = includes.Aggregate(entities, (current, include) => current.Include(include));
            }
            if (orderBy != null)
            {
                entities = orderBy(entities);
            }
            return entities;
        }
    }
}
