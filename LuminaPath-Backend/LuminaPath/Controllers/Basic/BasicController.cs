using Data.Interfaces;
using Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Controllers.Basic
{
	public class BasicController<T> : ControllerBase where T : class, IBasicInfo
	{
		private protected readonly IGenericCrud<T> _service;
		public BasicController(IGenericCrud<T> service)
		{
			_service = service;
		}

		[HttpGet]
		public IEnumerable<T> Get()
		{
			var entity = _service.GetAll();
			return entity.ToArray();
		}

		[HttpPost]
		public async Task<ActionResult<T>> PostAsync(T entity)
		{
			if (!ModelState.IsValid)
			{
				return NotFound();
			}
			var entityExists = await _service.GetByIdNoTrack(entity.Id);
			if (entityExists == null)
			{
				await _service.Create(entity);
				await _service.Save();
				return entity;
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

			return entity;
		}

		[HttpDelete]
		public async Task<IActionResult> DeleteAsync(int? id)
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

			return RedirectToPage("../Index");
		}

		private bool PersonalGamingExists(int id)
		{
			var entities = _service.GetAll();
			return entities.Any(e => e.Id == id);
		}
	}
}
