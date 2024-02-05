using AutoMapper;
using Domain.Common.Entities;
using Domain.Common.Interfaces;
using Domain.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Server.Controllers.Base
{
	[ApiController]
	//[Authorize(AuthenticationSchemes = "Bearer")]
	[Route("api/[controller]")]
	[Authorize]
	public abstract class GenericController<TEntity, TEntityDto>(GenericModelService<TEntity> service, IMapper mapper) : ControllerBase where TEntity : class, IBasicInfo
	{
		protected readonly GenericModelService<TEntity> _service = service;
		protected IEnumerable<string> Includes { get; set; } = new List<string>();
		public IMapper Mapper = mapper;

		[HttpGet]
		public async virtual Task<ActionResult<PaginatedResult<TEntityDto>>> Get([FromQuery] MediaFilter mediaFilter)
		{
			var result = await _service.GetAndMapEntities<TEntityDto>(mediaFilter, Includes);
			return result.Match<ActionResult<PaginatedResult<TEntityDto>>>(
				m => Ok(m),
				f => BadRequest(f));
		}

		[HttpGet("{id}")]
		public async virtual Task<ActionResult<TEntityDto>> GetById(int? id)
		{
			var result = await _service.GetById<TEntityDto>(id, Includes);
			return result.Match<ActionResult<TEntityDto>>(
				m => Ok(m),
				f => NotFound(f));
		}

		[HttpPost]
		public virtual async Task<ActionResult> PostAsync(TEntity viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var result = await _service.PostAsync<TEntityDto>(viewModel);
			return result.Match<ActionResult>(
				m => CreatedAtAction("GetById", new { id = viewModel.Id }, m),
				f => BadRequest(f)
				);
		}

		[HttpPut("{id}")]
		public virtual async Task<IActionResult> PutAsync(int id, TEntity viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (id != viewModel.Id)
			{
				return BadRequest("Id does not match entity");
			}

			var result = await _service.PutAsync<TEntityDto>(viewModel);
			return result.Match<IActionResult>(
				m => Ok(m),
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
