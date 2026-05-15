using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using LuminaPath.Infrastructure.Identity;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base
{
    abstract public class GenericMyModelService<TEntity> : IGenericMyModelService<TEntity> where TEntity : class, IBasicInfo, IMyMedia
    {
        protected readonly IObjectMapper _mapper;
        protected readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

        public virtual string[] Includes { get; set; } = [];
        protected abstract Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> DefaultOrderBy { get; }
        protected virtual string DuplicateMediaMessage => "This media is already added";

        public GenericMyModelService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper)
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
            return await QueryDefaultIncludes(query).ToListAsync();
        }

        public async Task<List<TEntity>> GetMyMedia(string UserId, Expression<Func<TEntity, bool>>? filter = null)
        {
            await using var context = await GetDbContextAsync();
            IQueryable<TEntity> entities = GetEntities(context);
            entities = entities.Where(g => g.LuminaUserId == UserId);
            entities = QueryDefaultIncludes(entities);
            entities = PrepareEntity(entities, filter, DefaultOrderBy);
            return await entities.ToListAsync();
        }

        public virtual async Task<PaginatedList<TEntity>> GetAllPaginated(
            string UserId,
            MediaFilter mediaFilter,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
        {
            if (orderBy == null)
                orderBy = DefaultOrderBy;
            Expression<Func<TEntity, bool>> userFilter = g => g.LuminaUserId == UserId;
            return await GetAllPaginated<TEntity>(mediaFilter, Includes, userFilter, orderBy);
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
            return CreatePaginatedList(mappedEntities, mediaFilter.Paging);
        }

        public virtual async Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null)
        {
            if (id == null)
                throw new Exception("No id given");

            await using (var dbContext = await GetDbContextAsync())
            {
                IQueryable<TEntity> entities = GetEntities(dbContext);
                if (includes != null)
                {
                    entities = includes.Aggregate(entities, (current, include) => current.Include(include));
                }

                return await entities.FirstOrDefaultAsync(e => e.Id == id) ?? throw new KeyNotFoundException("Entity not found");
            }
        }

        public async Task<Result<TEntity, FailedResult>> PostAsync(TEntity viewModel, ILuminaUser? user)
        {
            if (user == null)
                return new FailedResult("User not found, please login");
            if (await IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
            {
                return new FailedResult(DuplicateMediaMessage);
            }
            viewModel.LuminaUserId = user.Id;
            await PrepareForSave(viewModel, user);

            return await PostAsync(viewModel);
        }

        public async Task<Result<TEntity, FailedResult>> PutAsync(TEntity viewModel, ILuminaUser? user)
        {
            if (user == null)
                return new FailedResult("User not found, please login");
            if (await IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
            {
                return new FailedResult(DuplicateMediaMessage);
            }
            viewModel.LuminaUserId = user.Id;
            await PrepareForSave(viewModel, user);
            return await PutAsync(viewModel);
        }

        protected virtual Task PrepareForSave(TEntity viewModel, ILuminaUser user)
        {
            return Task.CompletedTask;
        }

        public async Task DeleteMyData(ILuminaUser user)
        {
            using var context = await GetDbContextAsync();
            var myEntities = GetEntities(context).Where(g => g.LuminaUserId == user.Id);
            context.RemoveRange(myEntities);
            await context.SaveChangesAsync();
        }

        private async Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity)
        {
            await using var dbContext = await GetDbContextAsync();
            var entities = GetEntities(dbContext);
            await entities.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        private async Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity)
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
            int pageIndex = paging.PageIndex;
            int pageSize = paging.Count > 0 ? paging.Count : 10;
            return await entities.ToPaginatedListAsync(pageIndex, pageSize);
        }

        protected virtual PaginatedList<TDto> CreatePaginatedList<TDto>(IEnumerable<TDto> entities, Paging paging)
        {
            int pageIndex = paging.PageIndex;
            if (paging.Count > 0)
            {
                return PaginatedList<TDto>.Create(entities, pageIndex, paging.Count);
            }
            return PaginatedList<TDto>.Create(entities, pageIndex, 10);
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

        private IQueryable<TEntity> QueryDefaultIncludes(IQueryable<TEntity> query)
        {
            return Includes.Aggregate(query, (current, include) => current.Include(include));
        }

        protected virtual async Task<bool> IsMediaAlreadyAdded(int id, int myId, string userId)
        {
            await using var context = await GetDbContextAsync();
            var entities = GetEntities(context);
            var userEntities = await entities.AsNoTracking()
                .Where(e => e.Id != myId && e.LuminaUserId == userId)
                .ToListAsync();

            return userEntities.Any(e => e.MediaId == id);
        }

        protected bool EntityExists(int id, LuminaPathDbContext dbContext)
        {
            return dbContext.Set<TEntity>().Any(e => e.Id == id);
        }
    }
}
