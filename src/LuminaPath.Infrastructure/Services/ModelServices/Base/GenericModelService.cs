using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base
{
    public class GenericModelService<TEntity> : IGenericModelService<TEntity> where TEntity : class, IBasicInfo
    {
        protected readonly IObjectMapper _mapper;
        protected readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

        public virtual string[] Includes { get; set; } = [];

        public GenericModelService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper)
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

        public virtual async Task<List<TEntity>> GetAll()
        {
            await using var dbContext = await GetDbContextAsync();
            IQueryable<TEntity> query = GetEntities(dbContext);
            return await QueryIncludes(query, Includes).ToListAsync();
        }

        public virtual async Task<PaginatedList<TEntity>> GetAllPaginated(
            MediaFilter mediaFilter,
            IEnumerable<string> includes,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
        {
            return await GetAllPaginated<TEntity>(mediaFilter, includes, filter, orderBy);
        }

        public virtual async Task<PaginatedList<Dto>> GetAllPaginated<Dto>(
            MediaFilter mediaFilter,
            IEnumerable<string> includes,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
        {
            await using var dbContext = await GetDbContextAsync();
            IQueryable<TEntity> entities = GetEntities(dbContext);
            entities = PrepareEntity(entities, filter, orderBy, includes);

            var paginatedEntities = await CreatePaginatedList(entities, mediaFilter.Paging);

            if (typeof(Dto) == typeof(TEntity))
            {
                return (PaginatedList<Dto>)(object)paginatedEntities;
            }

            var mappedEntities = _mapper.Map<IEnumerable<TEntity>, IEnumerable<Dto>>(paginatedEntities);
            return PaginationFactory.FromMapped(paginatedEntities, mappedEntities, mediaFilter.Paging);
        }

        public virtual async Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id), "No id given");

            await using (var dbContext = await GetDbContextAsync())
            {
                IQueryable<TEntity> entities = GetEntities(dbContext);
                entities = QueryIncludes(entities, includes);

                return await entities.FirstOrDefaultAsync(e => e.Id == id) ?? throw new KeyNotFoundException("Entity not found");
            }
        }

        public virtual async Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity)
        {
            await using var dbContext = await GetDbContextAsync();
            var entities = GetEntities(dbContext);
            await entities.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity)
        {
            var id = entity.Id;

            await using (var dbContext = await GetDbContextAsync())
            {
                var entities = GetEntities(dbContext);
                if (!await entities.AsNoTracking().AnyAsync(e => e.Id == id))
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
                    if (!EntityExists(id, dbContext))
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
                return new FailedResult("Entry not found");

            await using (var dbContext = await GetDbContextAsync())
            {
                var entities = GetEntities(dbContext);
                var entity = await entities.FirstOrDefaultAsync(entity => entity.Id == id.Value);
                if (entity == null)
                {
                    return new FailedResult("Entry not found");
                }

                entities.Remove(entity);
                await dbContext.SaveChangesAsync();

                return id.Value;
            }
        }

        protected virtual async Task<PaginatedList<TEntity>> CreatePaginatedList(IQueryable<TEntity> entities, Paging paging)
        {
            return await PaginationFactory.CreateAsync(entities, paging);
        }

        protected virtual PaginatedList<TDto> CreatePaginatedList<TDto>(IEnumerable<TDto> entities, Paging paging)
        {
            return PaginationFactory.Create(entities, paging);
        }

        protected virtual IQueryable<TEntity> PrepareEntity(
            IQueryable<TEntity> entities,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            IEnumerable<string>? includes = null)
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

        private static IQueryable<TEntity> QueryIncludes(IQueryable<TEntity> query, IEnumerable<string>? includes)
        {
            return includes?.Aggregate(query, (current, include) => current.Include(include)) ?? query;
        }

        protected bool EntityExists(int id, LuminaPathDbContext dbContext)
        {
            return dbContext.Set<TEntity>().Any(e => e.Id == id);
        }
    }
}
