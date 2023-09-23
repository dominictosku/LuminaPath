using AutoMapper;
using Data.Classes;
using Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Controllers.Basic
{
	[ApiController]
	[Authorize(AuthenticationSchemes = "Bearer")]
	[Route("api/[controller]")]
	[Authorize]
	public abstract class BasicController<T, T2> : ControllerBase where T : class, IBasicInfo
	{
		protected readonly IGenericRepo<T> _service;
		protected IEnumerable<string> _includes { get; set; } = new List<string>();
		public IMapper Mapper;
		public BasicController(IGenericRepo<T> service, IMapper mapper)
		{
			_service = service;
			Mapper = mapper;
		}

		[HttpGet]
		public async virtual Task<PaginatedResult<T2>> Get([FromQuery] MediaFIlter filter)
		{
			PaginatedList<T> entities = await _service.GetAll(filter, _includes);
			var entitiesDto = Mapper.Map<IEnumerable<T>, IEnumerable<T2>>(entities);
			return new PaginatedResult<T2>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		[HttpGet("All/{howMany}")]
		public virtual IEnumerable<T2> GetAll(int? howMany)
		{
			var entities = _service.GetAll(howMany, _includes);
			var entitiesDto = Mapper.Map<IEnumerable<T>, IEnumerable<T2>>(entities);
			return entitiesDto;
		}

		[HttpGet("{id}")]
		public async virtual Task<ActionResult<T2>> GetById(int? id)
		{
			if (id == null)
				return NotFound();
			var entity = await _service.GetByIdNoTrack(id);
			if(entity == null)
				return NotFound();
			return Ok(entity);
		}

		[HttpPost]
		public virtual async Task<ActionResult<T2>> PostAsync(T viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest();
			}
			var entity = viewModel;
			var entityDto = Mapper.Map<T2>(viewModel);
			await _service.Create(entity);
			await _service.Save();

			return CreatedAtAction("GetById", new { id = viewModel.Id }, entityDto);
		}

		[HttpPut("{id}")]
		public virtual async Task<IActionResult> PutAsync(int id, T viewModel)
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

		protected bool MyMediaExists(int id)
		{
			var entities = _service.GetAll();
			return entities.Any(e => e.Id == id);
		}
	}
}
