using AutoMapper;
using Core.Models.Gaming;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Security.Claims;
using Server.Controllers.Base;
using Server.Helper;
using Infrastructure.Dto.Blob;
using Infrastructure.Dto.Gaming;
using Infrastructure.Interfaces;
using Core.Entities;
using Infrastructure;

namespace Server.Controllers
{
	public class GamesController : MediaController<Game, GamesDto>
	{
		private readonly ILogger<GamesController> _logger;
		private readonly IUnitOfWork unitOfWork;

		public GamesController(IUnitOfWork unitOfWork, ILogger<GamesController> logger, IMapper mapper) : base(unitOfWork.GameRepo, mapper)
		{
			_logger = logger; ;
			this.unitOfWork = unitOfWork;
			Includes = new List<string>() { "Image" };
		}

		[HttpGet]
		[AllowAnonymous]
		public override async Task<PaginatedResult<GamesDto>> Get([FromQuery] MediaFilter mediaFilter)
		{
			string userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			PaginatedList<Game> entities;
			Expression<Func<Game, bool>>? filter = g => true;
			if (mediaFilter.SearchString != null)
			{
				filter = g => g.Name.Contains(mediaFilter.SearchString);
			}
			if (mediaFilter.MyMedia)
			{
				filter = filter.And(g => g.MyGames == null ? false : g.MyGames.Where(m => m.LuminaUserId == userId).Count() >= 1);
			}
			if (userId != null)
			{
				entities = await unitOfWork.GameRepo.GetAllPaginated(mediaFilter.Paging, userId, filter);
			}
			else
			{
				entities = await unitOfWork.GameRepo.GetAllPaginated(mediaFilter.Paging, filter, includes: Includes);
			}
			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}
	}
}
