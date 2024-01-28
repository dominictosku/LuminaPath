using Application.Controllers.Base;
using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Dto.Gaming;
using Domain.Models;
using Domain.Models.Gaming;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace Application.Controllers
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
