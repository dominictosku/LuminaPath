using AutoMapper;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LuminaPath.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class MyGamesController : BasicController<MyGame, MyGameDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public MyGamesController(IGenericCrud<MyGame> service, UserManager<LuminaUser> userManager,
			ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_userManager = userManager;
			_logger = logger;
		}

		[HttpPost]
		public override async Task<ActionResult<MyGameDto>> PostAsync(MyGameDto entityDto)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized("Please Login");
			LuminaUser user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound("User not found, please login");
			entityDto.LuminaUser = user;
			return await base.PostAsync(entityDto);
		}
	}
}
