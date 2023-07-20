using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LuminaUserController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;

		public LuminaUserController(UserManager<IdentityUser> userManager)
		{
			_userManager = userManager;
		}
	}
}
