using Data;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;

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
