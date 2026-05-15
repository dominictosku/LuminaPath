using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Identity;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base
{
    public interface IGenericMyModelService<TEntity> where TEntity : class, IBasicInfo, IMyMedia
    {
        string[] Includes { get; set; }

        Task<Result<int, FailedResult>> DeleteAsync(int? id);
        Task DeleteMyData(LuminaUser user);
        Task<List<TEntity>> GetAll();
        Task<PaginatedList<TEntity>> GetAllPaginated(string UserId, MediaFilter mediaFilter, Expression<Func<TEntity, bool>>? filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);
        Task<PaginatedList<Dto>> GetAllPaginated<Dto>(MediaFilter mediaFilter, IEnumerable<string> includes, Expression<Func<TEntity, bool>>? filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);
        Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null);
        Task<List<TEntity>> GetMyMedia(string UserId, Expression<Func<TEntity, bool>>? filter = null);
        Task<Result<TEntity, FailedResult>> PostAsync(TEntity viewModel, LuminaUser? user);
        Task<Result<TEntity, FailedResult>> PutAsync(TEntity viewModel, LuminaUser? user);
    }
}
