using Domain.Entities;
using Domain.Models.Gaming;
using System.Linq.Expressions;

namespace Infrastructure.Interfaces.Repositories
{
    public interface IMyGameRepository : IGenericRepository<MyGame>
    {
        Task<PaginatedList<MyGame>> GetAllPaginated(Paging paging, string UserId, Expression<Func<MyGame, bool>> filter = null, IEnumerable<string> includes = null);
    }
}