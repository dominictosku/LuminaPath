using LuminaPath.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models.Base
{
    public abstract class MyMedia : IBasicInfo
    {
        public int Id { get; set; }

        [Range(1, 10)]
        public short? Rating { get; set; }

        [Range(1, 4)]
        public int Priortiy { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public double? TimeSpend { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string LuminaUserId { get; set; } = string.Empty;

        public LuminaUser? LuminaUser { get; set; }
    }
}
