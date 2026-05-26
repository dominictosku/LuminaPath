using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos;

public class MediaVideoDto : IBasicInfo
{
    public int Id { get; set; }

    public int MediaId { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public MediaVideoKind Kind { get; set; }

    public int SortOrder { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StorageName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string StreamUrl => $"api/media-videos/{Id}/stream";
}

public class MediaVideoUpdateDto
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public MediaVideoKind Kind { get; set; }
}

public class MediaVideoReorderDto
{
    public int MediaId { get; set; }

    public List<int> VideoIds { get; set; } = [];
}
