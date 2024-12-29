using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Models;
using LuminaPath.Core.Common.Enums;
using System.Text.RegularExpressions;

namespace LuminaPath.Infrastructure.Services
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

		public virtual async Task<PaginatedList<Document>> GetAllPaginated(
			Paging paging,
			Expression<Func<Document, bool>> filter = null,
			Func<IQueryable<Document>, IOrderedQueryable<Document>> orderBy = null,
			IEnumerable<string> includes = null)
		{
			using var context = await GetDbContextAsync();
			IQueryable<Document> entities = context.Documents;
			entities = PrepareEntity(entities, filter, orderBy, includes);
			return await CreatePaginatedList(entities, paging);
		}

		public async Task<Result<Document, FailedResult>> CreateDocument(IBrowserFile file, Media? media = null)
		{
			Stream fs = file.OpenReadStream(MaxAllowedSize);
			try
			{
				string imageName = string.Empty;
				if(media is not null)
				{
					await DeleteDocument(media.Image);
					imageName = $"{media.Name}-{Guid.NewGuid()}";
                }
                else
				{
                    imageName = SanitizeFileName(file.Name);
                }
				var result = await _storage.UploadAsync(fs, imageName);
				if (result.Error)
				{
					_logger.LogError("Could not Upload file, error: {0}", result.Status);
					return new FailedResult("Could not upload file");
				}

				return new Document()
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

		public async Task DeleteDocument(Document document)
		{
			try
			{
				using var context = await GetDbContextAsync();
				var existingDocument = context.Documents.Single(d => d.Id == document.Id);
				if (existingDocument is null)
				{
					_logger.LogError("Could not find file, document: {0}", document.Name);
					return;
				}

				var result = await _storage.DeleteAsync(document.Name);
				if (result.Error)
				{
					_logger.LogError("Could not delete file, error: {0}", result.Status);
					return;
				}

				context.Documents.Remove(existingDocument);
				context.SaveChanges();
			}
			catch (Exception ex)
			{
				_logger.LogError("Could not Upload File. error: {ex}", ex);
			}
		}

        public async Task RenameMediaImage(Media media)
        {
            await _storage.RenameAsync(media.Image.Name, $"{media.Id}-{media.Name}");
        }

        public async Task RenameDocument(string oldName, string newName)
		{
			await _storage.RenameAsync(oldName, newName);
		}

		public async Task DeleteMediaDocument(Media entity)
		{
			using var context = await GetDbContextAsync();
			if (entity.Image is null)
				return;
			var image = entity.Image;
			entity.Image = null;
			context.Update(entity);
			context.SaveChanges();
			await DeleteDocument(image);
		}

        private string SanitizeFileName(string fileName)
        {
            // Remove any invalid characters from the file name
            return Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
        }

        protected virtual async Task<PaginatedList<Document>> CreatePaginatedList(IQueryable<Document> entities, Paging paging)
		{
			int pageIndex = paging.PageIndex;
			if (paging.Count > 0)
			{
				return await PaginatedList<Document>.CreateAsync(entities, 1, paging.Count);
			}
			return await PaginatedList<Document>.CreateAsync(entities, pageIndex, 10);
		}

		protected virtual IQueryable<Document> PrepareEntity(
			IQueryable<Document> entities,
			Expression<Func<Document, bool>> filter = null,
			Func<IQueryable<Document>, IOrderedQueryable<Document>> orderBy = null,
			IEnumerable<string> includes = null
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

		private static Expression<Func<Document, bool>> GetFilterExpression(MediaFilter mediaFilter, string? userId = null)
		{
			Expression<Func<Document, bool>> filter = g => true;

			if (mediaFilter.SearchString != null)
			{
				filter = g => g.Name.Contains(mediaFilter.SearchString);
			}

			return filter;
		}
	}
}
