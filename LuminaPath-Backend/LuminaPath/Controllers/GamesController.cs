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
	public class GamesController : BasicController<Game, GamesDto>
	{
		private readonly ILogger<GamesController> _logger;

		public GamesController(IGenericCrud<Game> service, ILogger<GamesController> logger, IMapper mapper) : base(service, mapper)
		{
			_logger = logger;
			_includes = new List<string> { "PersonalGamings" };
		}
	}
}
