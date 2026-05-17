namespace LuminaPath.Core.Entities
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;

        public bool HasNextPage => PageIndex < TotalPages;

        public static PaginatedList<T> Create(
            IEnumerable<T> source, int pageIndex, int pageSize)
        {
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Max(1, pageSize);

            var count = source.Count();
            var items = source.Skip(
                (pageIndex - 1) * pageSize)
                .Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }

    public class PaginatedResult<T>
    {
        public PaginatedResult(PaginatedList<T> list)
        {
            Data = list;
            PageIndex = list.PageIndex;
            TotalPages = list.TotalPages;
            TotalCount = list.TotalCount;
        }

        public IEnumerable<T> Data { get; set; }
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
