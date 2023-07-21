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
	}
}
