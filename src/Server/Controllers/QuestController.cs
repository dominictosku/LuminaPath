using AutoMapper;
using Core.Interfaces;
using Core.Models;
using Core.Models.Quests;
using Infrastructure.Dto.Quests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Controllers.Base.Generic;
using System.Security.Claims;

namespace Server.Controllers
{
    public class QuestsController : GenericController<GamesQuest, GamesQuestDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public QuestsController(IGenericRepository<GamesQuest> service, ILogger<GamesController> logger,
			IMapper mapper, UserManager<LuminaUser> userManager) : base(service, mapper)
		{
			_logger = logger;
			_userManager = userManager;
		}

		[HttpPost]
		public override async Task<ActionResult<GamesQuestDto>> PostAsync(GamesQuest viewModel)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized("Please Login");
			LuminaUser user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound("User not found, please login");
			viewModel.Owner = user;
			return await base.PostAsync(viewModel);
		}
	}
}
