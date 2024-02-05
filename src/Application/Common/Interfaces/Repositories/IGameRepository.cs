using Domain.Common.Entities;
using Domain.Models;
using System.Linq.Expressions;

namespace Application.Common.Interfaces.Repositories
{
	public interface IGameRepository : IGenericRepository<Game>
	{
		Task<PaginatedList<Game>> GetAllPaginated(Paging paging, string UserId, Expression<Func<Game, bool>> filter = null, IEnumerable<string> includes = null);
	}
}