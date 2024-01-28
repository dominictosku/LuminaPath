using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
	public class PaginatedList<T> : List<T>
	{
		public int PageIndex { get; private set; }
		public int TotalPages { get; private set; }

		public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
		{
			PageIndex = pageIndex;
			TotalPages = (int)Math.Ceiling(count / (double)pageSize);

			AddRange(items);
		}

		public bool HasPreviousPage => PageIndex > 1;

		public bool HasNextPage => PageIndex < TotalPages;

		public static async Task<PaginatedList<T>> CreateAsync(
			IQueryable<T> source, int pageIndex, int pageSize)
		{
			var count = await source.CountAsync();
			var items = await source.Skip(
				(pageIndex - 1) * pageSize)
				.Take(pageSize).ToListAsync();
			return new PaginatedList<T>(items, count, pageIndex, pageSize);
		}
	}
	public class PaginatedResult<T>
	{
		public PaginatedResult(IEnumerable<T> items, int currentPage, int pages)
		{
			Data = items;
			CurrentPage = currentPage;
			Pages = pages;
		}
		public IEnumerable<T> Data { get; set; }
		public int CurrentPage { get; set; }
		public int Pages { get; set; }
	}
}
