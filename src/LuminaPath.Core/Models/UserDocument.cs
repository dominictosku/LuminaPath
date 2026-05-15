using LuminaPath.Core.Models.Base;

namespace LuminaPath.Core.Models
{
    public class UserDocument : Document
    {
        public string Album { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
