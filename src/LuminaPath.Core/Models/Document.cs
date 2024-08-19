using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Models
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
