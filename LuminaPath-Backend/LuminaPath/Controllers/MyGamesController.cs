using AutoMapper;
using Data.Classes;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto.Gaming;
using Data.Models.Gaming;
using Data.Repositories;
using LuminaPath.Controllers.Base;
using LuminaPath.Controllers.Base.Generic;
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
	[Authorize]
	public class MyGamesController : MyMediaController<MyGame, MyGameDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;
		private readonly MyGameRepo _myGameRepo;

		public MyGamesController(MyGameRepo service, UserManager<LuminaUser> userManager,
			ILogger<GamesController> logger, IMapper mapper) : base(service, mapper, userManager)
		{
			_userManager = userManager;
			_logger = logger;
			Includes = new List<string> { "Game" };
			_myGameRepo = service;
		}

		[HttpGet("games")]
		public async Task<ActionResult<PaginatedResult<GamesDto>>> GetGames([FromQuery] MediaFilter mediaFilter)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			PaginatedList<MyGame> entities = await _myGameRepo.GetAllPaginated(mediaFilter.Paging, user.Id);
			List<Game> games = new List<Game>();
			foreach (var myGame in entities)
			{
				games.Add(myGame.Game);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(games);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}
	}
}
