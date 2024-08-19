using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Common.Interfaces;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services
{
	public interface IGenericModelService<TEntity> where TEntity : class, IBasicInfo
	{
		Task<Result<int, FailedResult>> DeleteAsync(int? id);
		void Dispose();
		Task<PaginatedList<TEntity>> GetAllPaginated(MediaFilter mediaFilter, IEnumerable<string> includes, Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);
		Task<PaginatedList<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes);
        Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null);
		Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity);
		Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity);
	}
}