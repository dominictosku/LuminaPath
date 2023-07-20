using Data.Classes;
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

		[HttpGet("{username}")]
		public async Task<ActionResult<LuminaUser>> GetUser(string username)
		{
			IdentityUser? user = await _userManager.FindByNameAsync(username);

			if (user == null)
			{
				return NotFound();
			}

			return new LuminaUser
			{
				UserName = user.UserName ?? "Error",
				Email = user.Email ?? "Error"
			};
		}

		[HttpPost]
		public async Task<ActionResult<LuminaUser>> PostUser(LuminaUser user)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var result = await _userManager.CreateAsync(
				new IdentityUser() { UserName = user.UserName, Email = user.Email },
				user.Password
			);

			if (!result.Succeeded)
			{
				return BadRequest(result.Errors);
			}

			user.Password = null;
			return Created("", user);
		}
	}
}
