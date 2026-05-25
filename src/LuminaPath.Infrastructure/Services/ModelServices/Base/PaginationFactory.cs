using LuminaPath.Core.Entities;
using LuminaPath.Infrastructure.Extensions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base;

internal static class PaginationFactory
{
    public static Task<PaginatedList<TEntity>> CreateAsync<TEntity>(IQueryable<TEntity> query, Paging paging)
    {
        return query.ToPaginatedListAsync(paging.PageIndex, paging.EffectiveCount);
    }

    public static PaginatedList<TDto> Create<TDto>(IEnumerable<TDto> items, Paging paging)
    {
        return PaginatedList<TDto>.Create(items, paging.PageIndex, paging.EffectiveCount);
    }

    public static PaginatedList<TDto> FromMapped<TSource, TDto>(
        PaginatedList<TSource> source,
        IEnumerable<TDto> mappedItems,
        Paging paging)
    {
        return new PaginatedList<TDto>(
            mappedItems.ToList(),
            source.TotalCount,
            source.PageIndex,
            paging.EffectiveCount);
    }
}
