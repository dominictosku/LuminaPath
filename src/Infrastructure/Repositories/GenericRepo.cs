using Core;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
	public class GenericRepo<TEntity> : IGenericRepo<TEntity> where TEntity : class, IBasicInfo
	{
		private protected readonly LuminaPathDbContext _context;
		private protected readonly DbSet<TEntity> _entities;

		public GenericRepo(LuminaPathDbContext context)
		{
			_context = context;
			_entities = context.Set<TEntity>();
		}

		public virtual async Task<IEnumerable<TEntity>> GetAll(
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			IEnumerable<string> includes = null)
		{
			IQueryable<TEntity> entities = _entities;

			entities = PrepareEntity(entities, filter, orderBy, includes);

			if (orderBy != null)
			{
				return await orderBy(entities).ToListAsync();
			}
			else
			{
				return entities.ToList();
			}
		}

		public virtual async Task<PaginatedList<TEntity>> GetAllPaginated(
			Paging paging,
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			IEnumerable<string> includes = null)
		{
			IQueryable<TEntity> entities = _entities;
			entities = PrepareEntity(entities, filter, orderBy, includes);
			return await CreatePaginatedList(entities, paging);
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
				entities = includes.Aggregate(_entities.AsQueryable(), (current, include) => current.Include(include));
			}
			if (orderBy != null)
			{
				entities = orderBy(entities);
			}
			return entities;
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

		public IEnumerable<TEntity> GetAllNoTrack() =>
			_entities.AsNoTracking().ToList();

		public async Task<TEntity> GetById(int? id, IEnumerable<string> includes = null)
		{
			IQueryable<TEntity> entities = _entities;
			if (includes != null)
			{
				entities = includes.Aggregate(_entities.AsQueryable(), (current, include) => current.Include(include));
			}
			return await entities.FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("id not found");
		}

		public async Task<TEntity> GetByIdNoTrack(int? id) =>
			await _entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("id not found");

		#region CRUD
		public async Task Create(TEntity entity) =>
			await _context.AddAsync(entity);

		public void Update(TEntity entity)
		{
			_entities.Attach(entity);
			_context.Entry(entity).State = EntityState.Modified;
		}

		public async Task Delete(int? id)
		{
			TEntity existing = await _entities.FindAsync(id) ?? throw new Exception("id not found"); ;
			_entities.Remove(existing);
		}

		public async Task Save() =>
		  await _context.SaveChangesAsync();
	}
	#endregion
}
