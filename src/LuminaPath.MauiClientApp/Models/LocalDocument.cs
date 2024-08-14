using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.Models
{
    public class LocalDocument
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Uri { get; set; }
        public string? ContentType { get; set; }
        public DocumentType DocumentType { get; set; } = default!;

        public string? URL => $"/api/files/{Name}";
    }

    public enum DocumentType
    {
        Document,
        Image,
        PDF,
        Excel,
        Others
    }
}
