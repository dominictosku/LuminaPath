using Data.Classes;
using System.Linq.Expressions;

namespace Data.Interfaces
{
	public interface IGenericRepo<TEntity> where TEntity : class, IBasicInfo
	{
		Task<IEnumerable<TEntity>> GetAll(
			Expression<Func<TEntity, bool>> filter = null,
			Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
			string includeProperties = "");
		Task<IEnumerable<TEntity>> GetAll(int? howMany, IEnumerable<string> includes, Expression<Func<TEntity, bool>> filter = null);
		IEnumerable<TEntity> GetAllNoTrack();
		Task<PaginatedList<TEntity>> GetAllPaginated(
			MediaFIlter mediaFilter,
			Expression<Func<TEntity, bool>> filter = null,
			IEnumerable<string> includes = null);
		Task<TEntity> GetById(int? id, IEnumerable<string> includes = null);
		Task<TEntity> GetByIdNoTrack(int? id);
		Task Create(TEntity entity);
		Task Delete(int? id);
		Task Save();
		void Update(TEntity entity);
	}
}