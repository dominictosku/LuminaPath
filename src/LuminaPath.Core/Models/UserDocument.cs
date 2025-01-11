using LuminaPath.Core.Models.Base;

namespace LuminaPath.Core.Models
{
    public class UserDocument : Document
    {
        public string Album { get; set; } = string.Empty;
        public LuminaUser User { get; set; }
        public string UserId { get; set; }
    }
}
