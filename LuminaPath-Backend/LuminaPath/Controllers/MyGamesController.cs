using AutoMapper;
using Data.Classes;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
using Data.Repositories;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Authorization;
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

		[HttpGet("games")]
		public async Task<ActionResult<PaginatedResult<GamesDto>>> GetAsync([FromQuery] MediaFIlter filter)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			PaginatedList<MyGame> entities = await _myGameRepo.GetAllPaginated(filter, user.Id);
			List<Game> games = new List<Game>();
			foreach (var myGame in entities)
			{
				games.Add(myGame.Game);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(games);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		[HttpGet("games/All/{count}")]
		public async Task<ActionResult<IEnumerable<GamesDto>>> GetAllAsync(int? count)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			List<MyGame> entities = await _myGameRepo.GetAll(count ?? 100, user.Id);
			List<Game> games = new List<Game>();
			foreach (var myGame in entities)
			{
				games.Add(myGame.Game);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(games);
			return Ok(entitiesDto);
		}

		[HttpPost]
		public override async Task<ActionResult<MyGameDto>> PostAsync(MyGame viewModel)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			if (IsGameAlreadyAdded(viewModel.GameId, viewModel.Id, userId))
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
			if(IsGameAlreadyAdded(viewModel.GameId, viewModel.Id, userId))
			{
				return BadRequest("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PutAsync(id, viewModel);
		}

		private bool IsGameAlreadyAdded(int id, int myId, string userId)
		{
			var entities = _myGameRepo.GetAllNoTrack();
			return entities.Any(e => e.GameId == id && e.Id != myId && e.LuminaUserId == userId);
		}
	}
}
