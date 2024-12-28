using AutoMapper;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services
{
	public class GenericModelService<TEntity> : IGenericModelService<TEntity> where TEntity : class, IBasicInfo
	{
		protected readonly IMapper _mapper;
		protected readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

		public GenericModelService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IMapper mapper)
		{
			_dbContextFactory = dbContextFactory;
			_mapper = mapper;
		}

		protected async Task<LuminaPathDbContext> GetDbContextAsync()
		{
			return await _dbContextFactory.CreateDbContextAsync();
		}

		protected DbSet<TEntity> GetEntities(LuminaPathDbContext dbContext)
		{
			return dbContext.Set<TEntity>();
		}

        public async virtual Task<List<TEntity>> GetAll()
        {
            using (var dbContext = await GetDbContextAsync())
            {
                IQueryable<TEntity> entities = GetEntities(dbContext);
                return await entities.ToListAsync();
            }
        }

        public async virtual Task<PaginatedList<TEntity>> GetAllPaginated(MediaFilter mediaFilter, IEnumerable<string> includes,
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
		{
			using (var dbContext = await GetDbContextAsync())
			{
				IQueryable<TEntity> entities = GetEntities(dbContext);
				entities = PrepareEntity(entities, filter, orderBy, includes);
				return await CreatePaginatedList(entities, mediaFilter.Paging);
			}
		}

		public async virtual Task<PaginatedList<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes)
		{
			using (var dbContext = await GetDbContextAsync())
			{
				PaginatedList<TEntity> entities = await GetAllPaginated(mediaFilter, includes: includes);
				var entitiesDto = _mapper.Map<IEnumerable<TEntity>, IEnumerable<TDto>>(entities);
				return CreatePaginatedList<TDto>(entitiesDto, mediaFilter.Paging);
			}
		}

		public async virtual Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null)
		{
			if (id == null)
				throw new Exception("No id given");

			using (var dbContext = await GetDbContextAsync())
			{
				IQueryable<TEntity> entities = GetEntities(dbContext);
				if (includes != null)
				{
					entities = includes.Aggregate(entities, (current, include) => current.Include(include));
				}
				var entity = await entities.FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("id not found");
				return entity ?? throw new Exception("Entity not found");
			}
		}

		public virtual async Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity)
		{
			using (var dbContext = await GetDbContextAsync())
			{
				var entities = GetEntities(dbContext);
				await entities.AddAsync(entity);
				await dbContext.SaveChangesAsync();
				return entity;
			}
		}

		public virtual async Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity)
		{
			var id = entity.Id;

			using (var dbContext = await GetDbContextAsync())
			{
				var entities = GetEntities(dbContext);
				var existingEntity = await entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("id not found");
				if (existingEntity == null)
				{
					return new FailedResult("Entity not found");
				}

				dbContext.Update(entity);

				try
				{
					await dbContext.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!MyMediaExists(id, dbContext))
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
		}

		public virtual async Task<Result<int, FailedResult>> DeleteAsync(int? id)
		{
			if (id == null)
			{
				return new FailedResult("Entry not found");
			}

			using (var dbContext = await GetDbContextAsync())
			{
				var personalGaming = await GetById(id);

				if (personalGaming != null)
				{
					var entities = GetEntities(dbContext);
					TEntity existing = await entities.FindAsync(id) ?? throw new Exception("id not found");
					entities.Remove(existing);
					await dbContext.SaveChangesAsync();
				}

				return (int)id;
			}
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

		protected bool MyMediaExists(int id, LuminaPathDbContext dbContext)
		{
			var entities = dbContext.Set<TEntity>().ToList();
			return entities.Any(e => e.Id == id);
		}
	}
}
