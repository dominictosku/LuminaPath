using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class DocumentService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly IStorageService _storage;
        private readonly ILogger<DocumentService> _logger;
        private const long MaxAllowedSize = 3145728;

        public DocumentService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IStorageService storage, ILogger<DocumentService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _storage = storage;
            _logger = logger;
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
            using var context = await GetDbContextAsync();
            IQueryable<MediaDocument> entities = context.MediaDocuments;
            entities = PrepareEntity(entities, filter, orderBy, includes);
            return await CreatePaginatedList(entities, paging);
        }

        public async Task<Result<MediaDocument, FailedResult>> CreateDocument(IBrowserFile file, IMedia<MediaDocument>? media = null)
        {
            Stream fs = file.OpenReadStream(MaxAllowedSize);
            try
            {
                string imageName = string.Empty;
                if (media is not null)
                {
                    await DeleteDocument(media.Image);
                    imageName = $"{media.Name}-{Guid.NewGuid()}";
                }
                else
                {
                    imageName = SanitizeFileName(file.Name);
                }
                var result = await _storage.UploadAsync(fs, imageName, file.ContentType);
                if (result.Error)
                {
                    _logger.LogError("Could not Upload file, error: {0}", result.Status);
                    return new FailedResult("Could not upload file");
                }

                return new MediaDocument()
                {
                    Name = result.Blob.Name,
                    Description = "Image for media",
                    Path = string.Empty,
                    ContentType = result.Blob.ContentType,
                    DocumentType = DocumentType.Image
                };

            }
            catch (Exception ex)
            {
                _logger.LogError("Could not Upload File. error: {ex}", ex);
                return new FailedResult("Could not upload file");
            }
        }

        public async Task RenameMediaImage(Media media)
        {
            if (media.Image is null)
            {
                return;
            }

            var imageName = media.Image.Name;
            if (string.IsNullOrWhiteSpace(imageName))
            {
                return;
            }

            await _storage.RenameAsync(imageName, $"{media.Id}-{media.Name}");
        }

        public async Task RenameDocument(string oldName, string newName)
        {
            await _storage.RenameAsync(oldName, newName);
        }

        public async Task DeleteDocument(MediaDocument? document)
        {
            if (document is null)
            {
                return;
            }

            try
            {
                using var context = await GetDbContextAsync();
                var existingDocument = context.MediaDocuments.SingleOrDefault(d => d.Id == document.Id);
                if (existingDocument is null)
                {
                    _logger.LogError("Could not find file, document: {0}", document.Name);
                    return;
                }

                if (string.IsNullOrWhiteSpace(document.Name))
                {
                    return;
                }

                var result = await _storage.DeleteAsync(document.Name);
                if (result.Error)
                {
                    _logger.LogError("Could not delete file, error: {0}", result.Status);
                    return;
                }

                if (existingDocument.MediaId != null)
                {
                    var media = await context.Games.FindAsync(existingDocument.MediaId);
                    if (media != null)
                    {
                        media.Image = null;
                        context.Update(media);
                    }
                }

                context.Documents.Remove(existingDocument);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not Upload File. error: {ex}", ex);
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
                await DeleteDocument(image);
            }
            finally
            {
                if (ownsContext)
                {
                    await context.DisposeAsync();
                }
            }
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove any invalid characters from the file name
            return Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
        }

        protected virtual async Task<PaginatedList<MediaDocument>> CreatePaginatedList(IQueryable<MediaDocument> entities, Paging paging)
        {
            int pageIndex = paging.PageIndex;
            if (paging.Count > 0)
            {
                return await PaginatedList<MediaDocument>.CreateAsync(entities, pageIndex, paging.Count);
            }
            return await PaginatedList<MediaDocument>.CreateAsync(entities, pageIndex, 10);
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
