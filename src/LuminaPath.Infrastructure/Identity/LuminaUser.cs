using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using Microsoft.AspNetCore.Identity;

namespace LuminaPath.Infrastructure.Identity
{
    public class LuminaUser : IdentityUser, ILuminaUser
    {
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Hard sign-in gate set by admins. False means the user cannot
        /// sign in even with a correct password — enforced by
        /// <c>LuminaSignInManager.CanSignInAsync</c>. Distinct from
        /// Identity's <c>LockoutEnd</c> (which is for failed-login auto-
        /// lockouts that auto-expire); this flag stays false until an
        /// admin flips it. Defaults to true for code paths that do not
        /// require admin approval; the self-registration gate overrides it
        /// when approval is enabled.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public List<UserDocument> Documents { get; set; } = [];
        public LuminaUserInfo? LuminaUserInfo { get; set; }
        public List<MyGame>? MyGames { get; set; }
    }
}
