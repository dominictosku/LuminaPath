using Data;
using Data.Interfaces;
using Data.Models;
using LuminaPath.Controllers.Basic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Controllers
{
	public class GamesController : BasicController<Games>
	{
		private readonly ILogger<GamesController> _logger;

		public GamesController(IGenericCrud<Games> service, ILogger<GamesController> logger) : base(service)
		{
			_logger = logger;
		}
	}
}
