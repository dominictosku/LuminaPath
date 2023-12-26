using AutoMapper;
using Core.Classes;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Server.Controllers.Base.Generic
{
	[ApiController]
	//[Authorize(AuthenticationSchemes = "Bearer")]
	[Route("api/[controller]")]
	[Authorize]
	public abstract class GenericController<TEntity, TEntityDto> : ControllerBase where TEntity : class, IBasicInfo
	{
		protected readonly IGenericRepo<TEntity> _service;
		protected IEnumerable<string> Includes { get; set; } = new List<string>();
		public IMapper Mapper;
		public GenericController(IGenericRepo<TEntity> service, IMapper mapper)
		{
			_service = service;
			Mapper = mapper;
		}

		[HttpGet]
		public async virtual Task<PaginatedResult<TEntityDto>> Get([FromQuery] MediaFilter mediaFilter)
		{
			PaginatedList<TEntity> entities = await _service.GetAllPaginated(mediaFilter.Paging, includes: Includes);
			var entitiesDto = Mapper.Map<IEnumerable<TEntity>, IEnumerable<TEntityDto>>(entities);
			return new PaginatedResult<TEntityDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		[HttpGet("{id}")]
		public async virtual Task<ActionResult<TEntityDto>> GetById(int? id)
		{
			if (id == null)
				return NotFound();
			var entity = await _service.GetById(id, Includes);
			if (entity == null)
				return NotFound();
			var entitiesDto = Mapper.Map<TEntity, TEntityDto>(entity);
			return Ok(entitiesDto);
		}

		[HttpPost]
		public virtual async Task<ActionResult<TEntityDto>> PostAsync(TEntity viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest();
			}
			var entity = viewModel;
			var entityDto = Mapper.Map<TEntityDto>(viewModel);
			await _service.Create(entity);
			await _service.Save();

			return CreatedAtAction("GetById", new { id = viewModel.Id }, entityDto);
		}

		[HttpPut("{id}")]
		public virtual async Task<IActionResult> PutAsync(int id, TEntity viewModel)
		{
			if (id != viewModel.Id)
			{
				return BadRequest();
			}

			var entity = await _service.GetByIdNoTrack(id);
			if (entity == null)
			{
				return NotFound();
			}

			_service.Update(viewModel);

			try
			{
				await _service.Save();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!MyMediaExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return NoContent();
		}

		[HttpDelete]
		public virtual async Task<IActionResult> DeleteAsync(int? id)
		{
			if (id == null || _service.GetAll() == null)
			{
				return NotFound();
			}
			var personalGaming = await _service.GetById(id);

			if (personalGaming != null)
			{
				await _service.Delete(id);
				await _service.Save();
			}

			return new JsonResult("Ok");
		}

		protected async Task<(LuminaUser user, string UserId)> GetUserAndUserIdAsync(UserManager<LuminaUser> userManager)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return (null, null);

			LuminaUser user = await userManager.FindByIdAsync(userId);
			return (user, userId);
		}

		protected bool MyMediaExists(int id)
		{
			var entities = _service.GetAll().Result;
			return entities.Any(e => e.Id == id);
		}
	}
}
