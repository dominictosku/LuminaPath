using Core.Models.Gaming;
using Microsoft.AspNetCore.Identity;

namespace Core.Models
{
	public class LuminaUser : IdentityUser
	{
		public string? RefreshToken { get; set; }
		public List<MyGame>? PersonalGamings { get; set; }
	}
}
