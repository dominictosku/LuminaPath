using LuminaPath.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models
{
    public class Quest : IBasicInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter a description")] public string? Description { get; set; }
        public bool HasStartDate { get; set; } = false;
        public DateTime? StartDate { get; set; }
        public string? Location { get; set; }
        public LuminaUser? Owner { get; set; }
    }
}
