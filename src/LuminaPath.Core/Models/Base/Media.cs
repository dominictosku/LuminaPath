using LuminaPath.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models.Base
{
    [Index(nameof(Name), IsUnique = true)]
    public abstract class Media : IMedia<MediaDocument>
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Title")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string> Genres { get; set; } = new List<string>();

        [DataType(DataType.Date)]
        [Display(Name = "Release Date")]
        public DateTime? ReleaseDate { get; set; }

        public string Source { get; set; } = "Lumina";

        public MediaDocument? Image { get; set; }
    }
}
