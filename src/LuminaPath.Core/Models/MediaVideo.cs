using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models;

public class MediaVideo : IBasicInfo
{
    public int Id { get; set; }

    public int MediaId { get; set; }

    public Media? Media { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public MediaVideoKind Kind { get; set; }

    public int SortOrder { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [StringLength(260)]
    public string StorageName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
