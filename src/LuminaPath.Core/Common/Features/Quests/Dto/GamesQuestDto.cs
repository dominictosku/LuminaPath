using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Common.Features.Quests.Dto
{
    public class GamesQuestDto : IBasicInfo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter a description")]
        public string? Description { get; set; }
        public bool HasStartDate { get; set; } = false;
        public DateTime? StartDate { get; set; }
        public string? Location { get; set; }
        public LuminaUser? Owner { get; set; }
    }
}
