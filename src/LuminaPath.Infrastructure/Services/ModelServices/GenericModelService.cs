using AutoMapper;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class GenericModelService<TEntity> : IGenericModelService<TEntity> where TEntity : class, IBasicInfo
    {
        protected readonly IMapper _mapper;
        protected readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

        public virtual string[] Includes { get; set; }

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

        public virtual async Task<List<TEntity>> GetAll()
        {
            using (var dbContext = await GetDbContextAsync())
            {
                IQueryable<TEntity> query = GetEntities(dbContext);
                query = Includes.Aggregate(query, (current, include) => current.Include(include));
                return await query.ToListAsync();
            }
        }

        public virtual async Task<PaginatedList<TEntity>> GetAllPaginated(
            MediaFilter mediaFilter,
            IEnumerable<string> includes,
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            return await GetAllPaginated<TEntity>(mediaFilter, includes, filter, orderBy);
        }

        public virtual async Task<PaginatedList<Dto>> GetAllPaginated<Dto>(
            MediaFilter mediaFilter,
            IEnumerable<string> includes,
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            using (var dbContext = await GetDbContextAsync())
            {
                IQueryable<TEntity> entities = GetEntities(dbContext);
                entities = PrepareEntity(entities, filter, orderBy, includes);

                var paginatedEntities = await CreatePaginatedList(entities, mediaFilter.Paging);

                if (typeof(Dto) == typeof(TEntity))
                {
                    return paginatedEntities as PaginatedList<Dto>;
                }

                var mappedEntities = _mapper.Map<IEnumerable<TEntity>, IEnumerable<Dto>>(paginatedEntities);

                return CreatePaginatedList(mappedEntities, mediaFilter.Paging);
            }
        }

        public virtual async Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null)
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

                return await entities.FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("Entity not found");
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
                var existingEntity = await entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("Entity not found");

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

            using (var dbContext = await GetDbContextAsync())
            {
                var entity = await GetById(id);
                var entities = GetEntities(dbContext);

                entities.Remove(entity);
                await dbContext.SaveChangesAsync();

                return id.Value;
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
            IEnumerable<string> includes = null)
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

        protected bool EntityExists(int id, LuminaPathDbContext dbContext)
        {
            return dbContext.Set<TEntity>().Any(e => e.Id == id);
        }
    }
}
