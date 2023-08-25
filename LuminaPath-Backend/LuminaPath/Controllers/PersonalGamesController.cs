using AutoMapper;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
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
	public class PersonalGamesController : BasicController<PersonalGaming, PersonalGamingDto>
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ILogger<GamesController> _logger;

		public PersonalGamesController(IGenericCrud<PersonalGaming> service, UserManager<LuminaUser> userManager, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_userManager = userManager;
			_logger = logger;
		}

		[HttpPost]
		public override async Task<ActionResult<PersonalGamingDto>> PostAsync(PersonalGamingDto entityDto)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized("Please Login");
			LuminaUser user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound("User not found, please login");
			var entity = Mapper.Map<PersonalGaming>(entityDto);
			entity.LuminaUser = user;
			if (!ModelState.IsValid)
			{
				return NotFound("Item not found");
			}
			var entityExists = await _service.GetByIdNoTrack(entity.Id);
			if (entityExists == null)
			{
				await _service.Create(entity);
				await _service.Save();
				return entityDto;
			}
			_service.Update(entity);

			try
			{
				await _service.Save();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!PersonalGamingExists(entity.Id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return entityDto;
		}
	}
}
