using Application.Common.Interfaces.Repositories;
using AutoMapper;
using Domain.Common.Entities;
using Domain.Common.Entities.Results;
using Domain.Common.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
	public class GenericModelService<TEntity> where TEntity : class, IBasicInfo
	{
		public readonly IGenericRepository<TEntity> _repository;
		public readonly IMapper _mapper;

		public GenericModelService(IGenericRepository<TEntity> repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async virtual Task<Result<PaginatedResult<TDto>, FailedResult>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes)
		{
			PaginatedList<TEntity> entities = await _repository.GetAllPaginated(mediaFilter.Paging, includes: includes);
			var entitiesDto = _mapper.Map<IEnumerable<TEntity>, IEnumerable<TDto>>(entities);
			return new PaginatedResult<TDto>(entitiesDto, entities.PageIndex, entities.TotalPages);
		}

		public async virtual Task<Result<TEntity, FailedResult>> GetById(int? id, IEnumerable<string> includes)
		{
			if (id == null)
				return new FailedResult("No id was given");
			var entity = await _repository.GetById(id, includes);
			if (entity == null)
				return new FailedResult("No entity for this id was found");
			return entity;
		}

		public virtual async Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity)
		{
			await _repository.Create(entity);
			await _repository.Save();

			return entity;
		}

		public virtual async Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity)
		{
			var id = entity.Id;
			var existingEntity = await _repository.GetByIdNoTrack(id);
			if (existingEntity == null)
			{
				return new FailedResult("Entity not found");
			}

			_repository.Update(entity);

			try
			{
				await _repository.Save();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!MyMediaExists(id))
				{
					return new FailedResult("Entity could not be saved");
				}
				else
				{
					throw;
				}
			}

			return entity;
		}

		public virtual async Task<Result<int, FailedResult>> DeleteAsync(int? id)
		{
			if (id == null || _repository.GetAll() == null)
			{
				return new FailedResult("Entry not found");
			}
			var personalGaming = await _repository.GetById(id);

			if (personalGaming != null)
			{
				await _repository.Delete(id);
				await _repository.Save();
			}

			return (int)id;
		}

		protected bool MyMediaExists(int id)
		{
			var entities = _repository.GetAll().Result;
			return entities.Any(e => e.Id == id);
		}
	}
}
