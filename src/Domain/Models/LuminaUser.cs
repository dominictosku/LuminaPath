using Domain.Models.Gaming;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models
{
	public class LuminaUser : IdentityUser
	{
		public List<MyGame>? MyGames { get; set; }
	}
}
