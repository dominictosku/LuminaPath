using LuminaPath.Core.Entities;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices;

namespace LuminaPath.Features.Documents
{
    public class DocumentPageService
    {
        private readonly DocumentService _documentService;

        public DocumentPageService(DocumentService documentService)
        {
            _documentService = documentService;
        }

        public Task<PaginatedList<MediaDocument>> GetPage(int pageIndex, int count)
        {
            return _documentService.GetAllPaginated(new Paging(pageIndex, count));
        }

        public Task Delete(MediaDocument? document)
        {
            return _documentService.DeleteDocument(document);
        }

        public Task<MediaDocument> Create(MediaDocument document)
        {
            return _documentService.CreateMediaDocument(document);
        }

        public Task<MediaDocument> Update(MediaDocument document)
        {
            return _documentService.UpdateMediaDocument(document);
        }

        public async Task DeleteSelected(IEnumerable<MediaDocument> documents)
        {
            foreach (var document in documents.ToList())
            {
                await Delete(document);
            }
        }
    }
}
