using LuminaPath.Core.Models;

namespace LuminaPath.Core.Interfaces
{
    public interface IMyMedia : IBasicInfo
    {
        public int MediaId { get; }

        public string LuminaUserId { get; set; }

        public LuminaUser? LuminaUser { get; set; }
    }
}
