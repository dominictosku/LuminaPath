using AutoMapper;
using Domain.Dto.Quests;
using Domain.Models;
using Domain.Models.Quests;
using Infrastructure.Interfaces.Repositories;
using Infrastructure.Services;
using LuminaPath.Controllers.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LuminaPath.Controllers
{
    public class QuestsController : GenericController<GamesQuest, GamesQuestDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public QuestsController(IQuestRepository repository, ILogger<GamesController> logger,
			IMapper mapper, UserManager<LuminaUser> userManager) : base(new GenericModelService<GamesQuest>(repository, mapper), mapper)
		{
			_logger = logger;
			_userManager = userManager;
		}

		[HttpPost]
		public override async Task<ActionResult> PostAsync(GamesQuest viewModel)
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
