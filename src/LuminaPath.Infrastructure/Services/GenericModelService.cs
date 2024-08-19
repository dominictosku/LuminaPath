using AutoMapper;
using LuminaPath.Core.Models;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using LuminaPath.Infrastructure;

namespace LuminaPath.Infrastructure.Services
{
	public class GenericModelService<TEntity> : IDisposable, IGenericModelService<TEntity> where TEntity : class, IBasicInfo
	{
		protected readonly LuminaPathDbContext _dbContext;
		protected DbSet<TEntity> _entities;
		protected readonly IMapper _mapper;

		public GenericModelService(LuminaPathDbContext dbContext, IMapper mapper)
		{
			_dbContext = dbContext;
			_entities = dbContext.Set<TEntity>();
			_mapper = mapper;
		}

		public async virtual Task<PaginatedList<TEntity>> GetAllPaginated(MediaFilter mediaFilter, IEnumerable<string> includes,
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
		{
			IQueryable<TEntity> entities = _entities;
			entities = PrepareEntity(entities, filter, orderBy, includes: includes);
			return await CreatePaginatedList(entities, mediaFilter.Paging);
		}

		public async virtual Task<PaginatedList<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes)
		{
			PaginatedList<TEntity> entities = await GetAllPaginated(mediaFilter, includes: includes);
			var entitiesDto = _mapper.Map<IEnumerable<TEntity>, IEnumerable<TDto>>(entities);
			return CreatePaginatedList<TDto>(entitiesDto, mediaFilter.Paging);
        }

		public async virtual Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null)
		{
			if (id == null)
				throw new Exception("No id given");
			IQueryable<TEntity> entities = _entities;
			if (includes != null)
			{
				entities = includes.Aggregate(_entities.AsQueryable(), (current, include) => current.Include(include));
			}
			var entity = await entities.FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("id not found");
			return entity ?? throw new Exception("Entity not found");
		}

		public virtual async Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity)
		{
			await _dbContext.AddAsync(entity);
			await _dbContext.SaveChangesAsync();

			return entity;
		}

		public virtual async Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity)
		{
			var id = entity.Id;
			var existingEntity = await _entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("id not found");
			if (existingEntity == null)
			{
				return new FailedResult("Entity not found");
			}

			_entities.Attach(entity);
			_dbContext.Entry(entity).State = EntityState.Modified;

			try
			{
				await _dbContext.SaveChangesAsync();
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
			if (id == null)
			{
				return new FailedResult("Entry not found");
			}
			var personalGaming = await GetById(id);

			if (personalGaming != null)
			{
				TEntity existing = await _entities.FindAsync(id) ?? throw new Exception("id not found"); ;
				_entities.Remove(existing);
				await _dbContext.SaveChangesAsync();
			}

			return (int)id;
		}

		protected virtual async Task<PaginatedList<TEntity>> CreatePaginatedList(IQueryable<TEntity> entities, Paging paging)
		{
			int pageIndex = paging.PageIndex;
			if (paging.Count > 0)
			{
				return await PaginatedList<TEntity>.CreateAsync(entities, 1, paging.Count);
			}
			return await PaginatedList<TEntity>.CreateAsync(entities, pageIndex, 10);
		}

        protected virtual PaginatedList<TDto> CreatePaginatedList<TDto>(IEnumerable<TDto> entities, Paging paging)
        {
            int pageIndex = paging.PageIndex;
            if (paging.Count > 0)
            {
                return PaginatedList<TDto>.Create(entities, 1, paging.Count);
            }
            return PaginatedList<TDto>.Create(entities, pageIndex, 10);
        }

        protected virtual IQueryable<TEntity> PrepareEntity(
			IQueryable<TEntity> entities,
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			IEnumerable<string> includes = null
		)
		{
			if (filter != null)
			{
				entities = entities.Where(filter);
			}
			if (includes != null)
			{
				entities = includes.Aggregate(entities, (current, include) => current.Include(include));
			}
			if (orderBy != null)
			{
				entities = orderBy(entities);
			}
			return entities;
		}

		protected bool MyMediaExists(int id)
		{
			var entities = _entities.ToList();
			return entities.Any(e => e.Id == id);
		}

		public void Dispose()
		{
			_dbContext.Dispose();
		}
	}
}
