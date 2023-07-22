using AutoMapper;
using Data.Classes;
using Data.Interfaces;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LuminaPath.Controllers.Basic
{
	[ApiController]
	[Authorize(AuthenticationSchemes = "Bearer")]
	[Route("api/[controller]")]
	public abstract class BasicController<T, T2> : ControllerBase where T : class, IBasicInfo
	{
		private protected readonly IGenericCrud<T> _service;
		public IMapper Mapper;
		public BasicController(IGenericCrud<T> service, IMapper mapper)
		{
			_service = service;
			Mapper = mapper;
		}

		[HttpGet]
		[AllowAnonymous]
		public virtual IEnumerable<T2> Get()
		{
			var entities = _service.GetAll();
			var entitiesDto = Mapper.Map<IEnumerable<T>, IEnumerable<T2>>(entities);
			return entitiesDto.ToArray();
		}

		[HttpGet("{id}")]
		[AllowAnonymous]
		public async virtual Task<ActionResult<T2>> Get(int? id)
		{
			if (id == null)
				return NotFound();
			var entity = await _service.GetByIdNoTrack(id);
			if(entity == null)
				return NotFound();
			return Ok(entity);
		}

		[HttpPost]
		public virtual async Task<ActionResult<T2>> PostAsync(T2 entityDto)
		{
			if (!ModelState.IsValid)
			{
				return NotFound();
			}
			var entity = Mapper.Map<T>(entityDto);
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

		protected bool PersonalGamingExists(int id)
		{
			var entities = _service.GetAll();
			return entities.Any(e => e.Id == id);
		}
	}
}
