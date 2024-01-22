using AutoMapper;
using Core.Entities;
using Core.Models;
using Core.Models.Gaming;
using Infrastructure.Dto.Gaming;
using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Controllers.Base;
namespace Server.Controllers
{
    [Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class MyGamesController : MyMediaController<MyGame, MyGameDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;
		private readonly IUnitOfWork unitOfWork;

		public MyGamesController(IUnitOfWork unitOfWork, UserManager<LuminaUser> userManager,
			ILogger<GamesController> logger, IMapper mapper) : base(unitOfWork.MyGameRepo, mapper, userManager)
		{
			this.unitOfWork = unitOfWork;
			_userManager = userManager;
			_logger = logger;
			Includes = new List<string> { "Game" };
		}
	}
}
