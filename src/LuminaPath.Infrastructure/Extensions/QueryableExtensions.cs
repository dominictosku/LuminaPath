using LuminaPath.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source, int pageIndex, int pageSize)
        {
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Max(1, pageSize);

            var count = await source.CountAsync();
            var items = await source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
