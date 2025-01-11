using LuminaPath.Core.Enums;

namespace LuminaPath.Core.Interfaces
{
    public interface IDocument
    {
        string? ContentType { get; set; }
        string? Description { get; set; }
        DocumentType DocumentType { get; set; }
        int Id { get; set; }
        string? Name { get; set; }
        string? Path { get; set; }
    }
}