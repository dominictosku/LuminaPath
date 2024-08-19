using AutoMapper;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuminaPath.Infrastructure.Controllers.Base
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public abstract class GenericController<TEntity, TEntityDto>(IGenericModelService<TEntity> service, IMapper mapper) : ControllerBase
		where TEntity : class, IBasicInfo
		where TEntityDto : class, IBasicInfo
	{
		protected readonly IGenericModelService<TEntity> _service = service;
		protected IEnumerable<string> Includes { get; set; } = new List<string>();
		public IMapper Mapper = mapper;

		[HttpGet]
		public async virtual Task<ActionResult<PaginatedList<TEntityDto>>> Get([FromQuery] MediaFilter mediaFilter)
		{
			var result = await _service.GetAndMapEntities<TEntityDto>(mediaFilter, Includes);
			return Ok(result);
		}

		[HttpGet("{id}")]
		public async virtual Task<ActionResult<TEntityDto>> GetById(int? id)
		{
			var result = await _service.GetById(id, Includes);
			return Ok(Mapper.Map<TEntityDto>(result));
		}

		[HttpPost]
		public virtual async Task<ActionResult> PostAsync(TEntityDto viewModel)
		{
			var entity = Mapper.Map<TEntity>(viewModel);
			var result = await _service.PostAsync(entity);
			return result.Match<ActionResult>(
				m => CreatedAtAction("GetById", new { id = viewModel.Id }, Mapper.Map<TEntityDto>(m)),
				f => BadRequest(f)
				);
		}

		[HttpPut("{id}")]
		public virtual async Task<IActionResult> PutAsync(int id, TEntityDto viewModel)
		{
			if (id != viewModel.Id)
			{
				return BadRequest("Id does not match entity");
			}

			var entity = Mapper.Map<TEntity>(viewModel);
			var result = await _service.PutAsync(entity);
			return result.Match<IActionResult>(
				m => Ok(Mapper.Map<TEntityDto>(m)),
				f => BadRequest(f));
		}

		[HttpDelete]
		public virtual async Task<IActionResult> DeleteAsync(int? id)
		{
			var result = await _service.DeleteAsync(id);
			return result.Match<IActionResult>(
				m => Ok(),
				f => NotFound(f));
		}

		protected async Task<(LuminaUser? user, string? UserId)> GetUserAndUserIdAsync(UserManager<LuminaUser> userManager)
		{
			string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return (null, null);

			LuminaUser? user = await userManager.FindByIdAsync(userId);
			if (user == null)
				return (null, null);
			return (user, userId);
		}
	}
}
