using Domain.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using System.Linq.Expressions;
using Domain.Common.Entities.Results;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;

namespace Infrastructure.Services
{
	public class DocumentService
	{
		private readonly LuminaPathDbContext _context;
		private readonly IAzureStorage _storage;
		private readonly ILogger _logger;
        private const long MaxAllowedSize = 3145728;

        public DocumentService(LuminaPathDbContext context, IAzureStorage storage, ILogger logger) 
		{
			_context = context;
            _storage = storage;
            _logger = logger;
		}

		public virtual async Task<PaginatedList<Document>> GetAllPaginated(
			Paging paging,
			Expression<Func<Document, bool>> filter = null,
			Func<IQueryable<Document>, IOrderedQueryable<Document>> orderBy = null,
			IEnumerable<string> includes = null)
		{
			IQueryable<Document> entities = _context.Documents;
			entities = PrepareEntity(entities, filter, orderBy, includes);
			return await CreatePaginatedList(entities, paging);
		}

		public async Task<Result<Document, FailedResult>> CreateDocument(IBrowserFile file, string baseUrl)
		{
			Stream fs = file.OpenReadStream(MaxAllowedSize);
			try
			{
				string imageName = Guid.NewGuid().ToString();
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
					Uri = baseUrl + $"api/files/{result.Blob.Name}",
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
				var existingDocument = _context.Documents.Single(d => d.Id == document.Id);
				if(existingDocument is null)
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

				_context.Documents.Remove(existingDocument);
				_context.SaveChanges();
			}
			catch (Exception ex)
			{
				_logger.LogError("Could not Upload File. error: {ex}", ex);
			}
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
