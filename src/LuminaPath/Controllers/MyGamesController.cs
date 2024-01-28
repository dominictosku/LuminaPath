using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Dto.Gaming;
using Domain.Models;
using Domain.Models.Gaming;
using LuminaPath.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace LuminaPath.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class MyGamesController : GenericController<MyGame, MyGameDto>
	{
		private readonly IUnitOfWork unitOfWork;
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public MyGamesController(IUnitOfWork unitOfWork, UserManager<LuminaUser> userManager,
			ILogger<GamesController> logger, IMapper mapper) : base(unitOfWork.MyGameRepo, mapper)
		{
			this.unitOfWork = unitOfWork;
			_userManager = userManager;
			_logger = logger;
			Includes = new List<string> { "Game" };
		}

		[HttpPost]
		public override async Task<ActionResult<MyGameDto>> PostAsync(MyGame viewModel)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, userId))
			{
				return BadRequest("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PostAsync(viewModel);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> PutAsync(int id, MyGame viewModel)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, userId))
			{
				return BadRequest("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PutAsync(id, viewModel);
		}

		protected bool IsMediaAlreadyAdded(int id, int myId, string userId)
		{
			var entities = _service.GetAllNoTrack();
			return entities.Any(e => e.MediaId == id && e.Id != myId && e.LuminaUserId == userId);
		}
	}
}
