using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public partial class DocumentService
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

        protected virtual async Task<PaginatedList<MediaDocument>> CreatePaginatedList(IQueryable<MediaDocument> entities, Paging paging)
        {
            return await PaginationFactory.CreateAsync(entities, paging);
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
