using Data.Classes;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Data.Repositories
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

		public async Task<IEnumerable<TEntity>> GetAll(
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			string includeProperties = "")
		{
			IQueryable<TEntity> query = _entities;

			if (filter != null)
			{
				query = query.Where(filter);
			}

			foreach (var includeProperty in includeProperties.Split
				(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				query = query.Include(includeProperty);
			}

			if (orderBy != null)
			{
				return await orderBy(query).ToListAsync();
			}
			else
			{
				return query.ToList();
			}
		}

		public async Task<IEnumerable<TEntity>> GetAll(int? count, IEnumerable<string> includes, Expression<Func<TEntity, bool>> filter = null)
		{
			IQueryable<TEntity> entities = _entities;
			if (filter != null)
			{
				entities = entities.Where(filter);
			}
			foreach (var include in includes)
			{
				entities = _entities.Include(include);
			}
			return await entities.Take(count ?? 100).ToListAsync();
		}

		public IEnumerable<TEntity> GetAllNoTrack() =>
			_entities.AsNoTracking().ToList();

		public async Task<PaginatedList<TEntity>> GetAllPaginated(
			MediaFIlter mediaFilter,
			Expression<Func<TEntity, bool>> filter = null,
			IEnumerable<string> includes = null)
		{
			int pageIndex = mediaFilter.PageIndex;
			IQueryable<TEntity> entities = _entities;
			if (filter != null)
			{
				entities = entities.Where(filter);
			}
			if (includes != null)
			{
				entities = includes.Aggregate(_entities.AsQueryable(), (current, include) => current.Include(include));
			}
			return await PaginatedList<TEntity>.CreateAsync(entities, pageIndex, 10);
		}

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
