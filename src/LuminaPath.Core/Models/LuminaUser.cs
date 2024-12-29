using Microsoft.AspNetCore.Identity;

namespace LuminaPath.Core.Models
{
    public class LuminaUser : IdentityUser
    {
        public List<MyGame>? MyGames { get; set; }
    }
}
