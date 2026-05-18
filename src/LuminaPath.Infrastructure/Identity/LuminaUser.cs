using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using Microsoft.AspNetCore.Identity;

namespace LuminaPath.Infrastructure.Identity
{
    public class LuminaUser : IdentityUser, ILuminaUser
    {
        public string FullName { get; set; } = string.Empty;
        public List<UserDocument> Documents { get; set; } = [];
        public LuminaUserInfo? LuminaUserInfo { get; set; }
        public List<MyGame>? MyGames { get; set; }
    }
}
