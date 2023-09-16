using Data.Classes;

namespace Data.Interfaces
{
	public interface IGenericRepo<T> where T : class
	{
		IEnumerable<T> GetAll();
		IEnumerable<T> GetAll(string include);
		IEnumerable<T> GetAll(int? howMany, IEnumerable<string> includes);
		Task<PaginatedList<T>> GetAll(MediaFIlter filter);
		Task<PaginatedList<T>> GetAll(MediaFIlter filter, IEnumerable<string> includes);
		Task<T> GetById(int? id);
		Task<T> GetByIdNoTrack(int? id);
		Task Create(T entity);
		void Update(T entity);
		Task Delete(int? id);
		Task Save();
	}
}
