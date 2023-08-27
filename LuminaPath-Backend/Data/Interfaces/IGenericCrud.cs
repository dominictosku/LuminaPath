using Data.Classes;

namespace Data.Interfaces
{
	public interface IGenericCrud<T> where T : class
	{
		IEnumerable<T> GetAll();
		IEnumerable<T> GetAll(string include);
		IEnumerable<T> GetAll(int? howMany, string include);
		Task<PaginatedList<T>> GetAll(MediaFIlter filter);
		Task<T> GetById(int? id);
		Task<T> GetByIdNoTrack(int? id);
		Task Create(T entity);
		void Update(T entity);
		Task Delete(int? id);
		Task Save();
	}
}
