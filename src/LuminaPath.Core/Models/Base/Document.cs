using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;

namespace LuminaPath.Core.Models.Base
{
    public class Document : IDocument
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Path { get; set; }
        public string? ContentType { get; set; }
        public DocumentType DocumentType { get; set; } = default!;
        public string Url => $"api/files/{Name}";
    }
}
