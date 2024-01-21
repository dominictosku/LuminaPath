using Core.Models.Gaming;
using Microsoft.AspNetCore.Identity;

namespace Core.Models
{
	public class LuminaUser : IdentityUser
	{
		public List<MyGame>? MyGames { get; set; }
	}
}
