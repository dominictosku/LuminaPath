using Domain.Common.Entities;
using Domain.Models;
using System.Linq.Expressions;

namespace Application.Common.Interfaces.Repositories
{
	public interface IMyGameRepository : IGenericRepository<MyGame>
	{
		Task<PaginatedList<MyGame>> GetAllPaginated(Paging paging, string UserId, Expression<Func<MyGame, bool>> filter = null, IEnumerable<string> includes = null);
	}
}