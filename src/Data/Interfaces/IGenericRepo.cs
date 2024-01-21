using Core.Entities;
using System.Linq.Expressions;

namespace Core.Interfaces
{
	public interface IGenericRepo<TEntity> where TEntity : class, IBasicInfo
	{
		Task<IEnumerable<TEntity>> GetAll(
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			IEnumerable<string> includes = null);
		Task<PaginatedList<TEntity>> GetAllPaginated(
			Paging paging,
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			IEnumerable<string> includes = null);
		IEnumerable<TEntity> GetAllNoTrack();
		Task<TEntity> GetById(int? id, IEnumerable<string> includes = null);
		Task<TEntity> GetByIdNoTrack(int? id);
		Task Create(TEntity entity);
		Task Delete(int? id);
		Task Save();
		void Update(TEntity entity);
	}
}