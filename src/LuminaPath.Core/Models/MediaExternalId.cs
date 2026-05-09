using LuminaPath.Core.Enums;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models;

public class MediaExternalId
{
    public int Id { get; set; }

    public int MediaId { get; set; }

    public Media? Media { get; set; }

    public ExternalMediaProvider Provider { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(128)]
    public string ExternalId { get; set; } = string.Empty;
}
