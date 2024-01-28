using Application.Controllers.Base;
using Application.Helper;
using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Dto.Gaming;
using Domain.Entities;
using Domain.Models.Gaming;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Application.Controllers
{
    public class GamesController : GenericController<Game, GamesDto>
	{
		private readonly IUnitOfWork unitOfWork;
		private readonly ILogger<GamesController> _logger;

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
			string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

			PaginatedList<Game> entities = userId != null
				? await unitOfWork.GameRepo.GetAllPaginated(mediaFilter.Paging, userId, filter)
				: await unitOfWork.GameRepo.GetAllPaginated(mediaFilter.Paging, filter, includes: Includes);

			var entitiesDto = Mapper.Map<IEnumerable<Game>, IEnumerable<GamesDto>>(entities);
			return new PaginatedResult<GamesDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		private Expression<Func<Game, bool>> GetFilterExpression(MediaFilter mediaFilter, string? userId)
		{
			Expression<Func<Game, bool>> filter = g => true;

			if (mediaFilter.SearchString != null)
			{
				filter = g => g.Name.Contains(mediaFilter.SearchString);
			}

			if (mediaFilter.MyMedia && userId != null)
			{
				filter = filter.And(g => g.MyGames != null && g.MyGames.Any(m => m.LuminaUserId == userId));
			}

			return filter;
		}
	}
}
