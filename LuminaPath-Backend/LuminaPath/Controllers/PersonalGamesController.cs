using Data.Interfaces;
using Data.Models;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PersonalGamesController : BasicController<PersonalGaming>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public PersonalGamesController(IGenericCrud<PersonalGaming> service, UserManager<LuminaUser> userManager, ILogger<GamesController> logger) : base(service)
		{
			_userManager = userManager;
			_logger = logger;
		}

		[HttpPost]
		public override async Task<ActionResult<PersonalGaming>> PostAsync(PersonalGaming entity)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized("Please Login");
			LuminaUser user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound("User not found, please create an account");
			entity.LuminaUser = user;
			return await base.PostAsync(entity);
		}
	}
}
