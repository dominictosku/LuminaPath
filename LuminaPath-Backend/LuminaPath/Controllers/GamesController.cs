using AutoMapper;
using Data;
using Data.Interfaces;
using Data.Models;
using Data.Models.Dto;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Controllers
{
	public class GamesController : BasicController<Games, GamesDto>
	{
		private readonly ILogger<GamesController> _logger;

		public GamesController(IGenericCrud<Games> service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_logger = logger;

		}

		public override async Task<ActionResult<GamesDto>> PostAsync(GamesDto entityDto)
		{
			if (NameAlreadyExists(entityDto.Name ?? ""))
			{
				return BadRequest("Title is already registered");
			}
			return await base.PostAsync(entityDto);
		}

		protected bool NameAlreadyExists(string name)
		{
			var entities = _service.GetAll();
			return entities.Any(e => e.Name == name);
		}
	}
}
