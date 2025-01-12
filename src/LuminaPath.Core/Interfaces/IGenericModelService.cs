using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using System.Linq.Expressions;

namespace LuminaPath.Core.Interfaces
{
    public interface IGenericModelService<TEntity> : IBaseModelService<TEntity> where TEntity : class, IBasicInfo
    {
        Task<Result<int, FailedResult>> DeleteAsync(int? id);
        Task<PaginatedList<TEntity>> GetAllPaginated(MediaFilter mediaFilter, IEnumerable<string> includes, Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);
        Task<PaginatedList<TDto>> GetAllPaginated<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes, Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);
        Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null);
        Task<Result<TEntity, FailedResult>> PostAsync(TEntity entity);
        Task<Result<TEntity, FailedResult>> PutAsync(TEntity entity);
    }
}