using AutoMapper;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
using Data.Repositories;
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
		private readonly MyGameRepo _myGameRepo;

		public MyGamesController(MyGameRepo service, UserManager<LuminaUser> userManager,
			ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_userManager = userManager;
			_logger = logger;
			_includes = new List<string> { "Game" };
			_myGameRepo = service;
		}

		[HttpPost]
		public override async Task<ActionResult<MyGameDto>> PostAsync(MyGame viewModel)
		{
			if (IsGameAlreadyAdded(viewModel.GameId, viewModel.Id))
			{
				return BadRequest();
			}
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized("Please Login");
			LuminaUser user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound("User not found, please login");
			viewModel.LuminaUser = user;
			return await base.PostAsync(viewModel);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> PutAsync(int id, MyGame viewModel)
		{
			if(IsGameAlreadyAdded(viewModel.GameId, viewModel.Id))
			{
				return BadRequest("This is game already added");
			}
			return await base.PutAsync(id, viewModel);
		}

		private bool IsGameAlreadyAdded(int id, int myId)
		{
			var entities = _myGameRepo.GetAllNoTrack();
			return entities.Any(e => e.GameId == id && e.Id != myId);
		}
	}
}
