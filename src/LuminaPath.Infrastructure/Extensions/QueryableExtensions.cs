using LuminaPath.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        private static readonly HashSet<string> OrderingMethodNames = new(StringComparer.Ordinal)
        {
            nameof(Queryable.OrderBy),
            nameof(Queryable.OrderByDescending),
            nameof(Queryable.ThenBy),
            nameof(Queryable.ThenByDescending)
        };

        private static readonly HashSet<string> OrderingResetMethodNames = new(StringComparer.Ordinal)
        {
            nameof(Queryable.Distinct),
            nameof(Queryable.GroupBy)
        };

        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source, int pageIndex, int pageSize)
        {
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Clamp(pageSize, 1, Paging.MaxCount);

            var count = await source.CountAsync();
            var items = await source
                .OrderByIdWhenUnordered()
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        private static IQueryable<T> OrderByIdWhenUnordered<T>(this IQueryable<T> source)
        {
            return HasOrdering(source.Expression)
                ? source
                : source.OrderBy(entity => EF.Property<int>(entity!, "Id"));
        }

        private static bool HasOrdering(Expression expression)
        {
            while (expression is MethodCallExpression methodCall)
            {
                if (methodCall.Method.DeclaringType == typeof(Queryable)
                    && OrderingMethodNames.Contains(methodCall.Method.Name))
                {
                    return true;
                }

                if (methodCall.Method.DeclaringType == typeof(Queryable)
                    && OrderingResetMethodNames.Contains(methodCall.Method.Name))
                {
                    return false;
                }

                expression = methodCall.Arguments[0];
            }

            return false;
        }
    }
}
