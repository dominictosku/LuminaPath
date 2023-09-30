using Data.Classes;

namespace Data.Interfaces
{
	public interface IGenericRepo<T> where T : class, IBasicInfo
	{
		Task Create(T entity);
		Task Delete(int? id);
		IEnumerable<T> GetAll();
		IEnumerable<T> GetAll(IEnumerable<string> includes);
		IEnumerable<T> GetAll(int? howMany, IEnumerable<string> includes);
		IEnumerable<T> GetAll(string include);
		IEnumerable<T> GetAllNoTrack();
		Task<PaginatedList<T>> GetAllPaginated(MediaFIlter filter);
		Task<PaginatedList<T>> GetAllPaginated(MediaFIlter filter, IEnumerable<string> includes);
		Task<T> GetById(int? id);
		Task<T> GetById(int? id, string include);
		Task<T> GetById(int? id, IEnumerable<string> includes);
		Task<T> GetByIdNoTrack(int? id);
		Task Save();
		void Update(T entity);
	}
}
