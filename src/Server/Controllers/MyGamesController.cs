using Application.Common.Features.Gaming.Dto;
using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Models;
using Infrastructure.Services;
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
	public class MyGamesController : GenericController<MyGame, MyGameDto>
	{
		private readonly new MyGameService _service;
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public MyGamesController(MyGameService service, UserManager<LuminaUser> userManager,
			ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_service = service;
			_userManager = userManager;
			_logger = logger;
			Includes = new List<string> { "Game" };
		}

		[HttpPost]
		public override async Task<ActionResult> PostAsync(MyGame viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			var result = await _service.PostAsync<MyGameDto>(viewModel, user);
			return result.Match<ActionResult>(
				m => CreatedAtAction("GetById", new { id = viewModel.Id }, m),
				f => BadRequest(f)
				);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> PutAsync(int id, MyGame viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (id != viewModel.Id)
			{
				return BadRequest("Id does not match entity");
			}

			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			var result = await _service.PutAsync<MyGameDto>(viewModel, user);
			return result.Match<IActionResult>(
				m => Ok(m),
				f => BadRequest(f));
		}
	}
}
