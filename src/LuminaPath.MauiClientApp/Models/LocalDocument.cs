using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Common.Enums;

namespace LuminaPath.MauiClientApp.Models
{
    public class LocalDocument : IDocument
	{
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Path { get; set; }
        public string? ContentType { get; set; }
        public DocumentType DocumentType { get; set; } = default!;
    }
}
