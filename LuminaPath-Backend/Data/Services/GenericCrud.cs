using Data;
using Data.Classes;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Data.Services
{
	public class GenericCrud<T> : IGenericCrud<T> where T : class, IBasicInfo
	{
		private readonly LuminaPathDbContext _context;
		private readonly DbSet<T> _entities;

		public GenericCrud(LuminaPathDbContext context)
		{
			_context = context;
			_entities = context.Set<T>();
		}
		public IEnumerable<T> GetAll() =>
			_entities.ToList();
		public IEnumerable<T> GetAll(string include) => 
			_entities.ToList();

		public IEnumerable<T> GetAll(int? howMany, string include) =>
			_entities.Take(howMany ?? 100).ToList();

		public async Task<PaginatedList<T>> GetAll(MediaFIlter filter)
		{
			return await PaginatedList<T>.CreateAsync(_entities, filter?.PageIndex ?? 1, 10);
		}

		public async Task<T> GetById(int? id) =>
			 await _entities.FindAsync(id);

		public async Task<T> GetByIdNoTrack(int? id) =>
			await _entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

		public async Task Create(T entity) =>
			await _context.AddAsync(entity);

		public void Update(T entity)
		{
			_entities.Attach(entity);
			_context.Entry(entity).State = EntityState.Modified;
		}

		public async Task Delete(int? id)
		{
			T existing = await _entities.FindAsync(id);
			_entities.Remove(existing);
		}

		public async Task Save() =>
		  await _context.SaveChangesAsync();

	}
}
