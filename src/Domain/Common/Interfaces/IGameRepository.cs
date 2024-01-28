using Domain.Entities;
using Domain.Models.Gaming;
using System.Linq.Expressions;

namespace Domain.Common.Interfaces
{
	public interface IGameRepository : IGenericRepository<Game>
	{
		Task<PaginatedList<Game>> GetAllPaginated(Paging paging, string UserId, Expression<Func<Game, bool>> filter = null, IEnumerable<string> includes = null);
	}
}