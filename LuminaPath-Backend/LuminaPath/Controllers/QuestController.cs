using AutoMapper;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
using Data.Models.Quests;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Controllers
{

	public class QuestsController : BasicController<GamesQuest, GamesQuestDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public QuestsController(IGenericCrud<GamesQuest> service, ILogger<GamesController> logger,
			IMapper mapper, UserManager<LuminaUser> userManager) : base(service, mapper)
		{
			_logger = logger;
			_userManager = userManager;
		}

		[HttpPost]
		public override async Task<ActionResult<GamesQuestDto>> PostAsync(GamesQuestDto entityDto)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized("Please Login");
			LuminaUser user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound("User not found, please login");
			entityDto.Owner = user;
			return await base.PostAsync(entityDto);
		}
	}
}
