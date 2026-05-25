using LuminaPath.Core.Entities;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base;

public abstract partial class MediaModelService<TMedia, TUserMedia> : GenericModelService<TMedia>
    where TMedia : Media
    where TUserMedia : MyMedia, IMyMedia
{
    public virtual async Task<List<TMedia>> GetDropdownMedia(string? searchName = null)
    {
        await using var context = await GetDbContextAsync();
        IQueryable<TMedia> query = context.Set<TMedia>().Include(media => media.Image);

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(media => media.Name.Contains(searchName));
        }

        return await query
            .OrderBy(media => media.Name)
            .ToListAsync();
    }

    protected async Task<List<TMedia>> GetDropdownParentMedia(
        Expression<Func<TMedia, bool>> parentFilter,
        string? searchName = null,
        int? excludeId = null)
    {
        await using var context = await GetDbContextAsync();
        IQueryable<TMedia> query = context.Set<TMedia>()
            .Include(media => media.Image)
            .Where(parentFilter);

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(media => media.Name.Contains(searchName));
        }

        if (excludeId is int id && id > 0)
        {
            query = query.Where(media => media.Id != id);
        }

        return await query.OrderBy(media => media.Name).ToListAsync();
    }

    protected async Task<List<TMedia>> GetChildMediaAsync(
        Expression<Func<TMedia, bool>> childFilter,
        CancellationToken cancellationToken = default,
        bool newestFirst = false)
    {
        await using var context = await GetDbContextAsync();
        var query = context.Set<TMedia>()
            .AsNoTracking()
            .Include(media => media.Image)
            .Where(childFilter);

        var ordered = newestFirst
            ? query.OrderByDescending(media => media.ReleaseDate).ThenBy(media => media.Name)
            : query.OrderBy(media => media.ReleaseDate).ThenBy(media => media.Name);

        return await ordered.ToListAsync(cancellationToken);
    }

    public virtual async Task<PaginatedList<TMedia>> GetAllPaginated(
        MediaFilter mediaFilter,
        string userId,
        Expression<Func<TMedia, bool>>? filter = null,
        IEnumerable<string>? includes = null)
    {
        await using var context = await GetDbContextAsync();
        IQueryable<TMedia> entities = GetEntities(context).Include(media => media.Image);
        entities = IncludeUserLibrary(entities, userId);

        var composedFilter = ComposeFilter(mediaFilter, userId, filter);
        entities = PrepareEntity(entities, composedFilter, query => ApplyOrdering(query, mediaFilter, userId), includes);
        return await CreatePaginatedList(entities, mediaFilter.Paging);
    }

    public virtual async Task<PaginatedList<TDto>> GetAndMapEntities<TDto>(
        MediaFilter mediaFilter,
        IEnumerable<string> includes,
        string? userId)
    {
        var filter = BuildFilterExpression(mediaFilter, userId);

        PaginatedList<TMedia> entities = userId != null
            ? await GetAllPaginated(mediaFilter, userId, filter)
            : await GetAllPaginated<TMedia>(mediaFilter, GetDefaultIncludes(includes), filter, query => ApplyOrdering(query, mediaFilter, userId));

        var entitiesDto = _mapper.Map<IEnumerable<TMedia>, IEnumerable<TDto>>(entities);
        return PaginationFactory.FromMapped(entities, entitiesDto, mediaFilter.Paging);
    }

    public virtual async Task<TDto> GetByIdAndMap<TDto>(int? id, IEnumerable<string> includes, string? userId)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id), "No id given");
        }

        await using var context = await GetDbContextAsync();
        IQueryable<TMedia> query = context.Set<TMedia>().Include(media => media.Image);

        if (userId != null)
        {
            query = IncludeUserLibrary(query, userId);
            query = IncludeNonLibraryNavigations(query, includes);
        }
        else
        {
            query = IncludeNavigations(query, includes);
        }

        var entity = await query.FirstOrDefaultAsync(media => media.Id == id.Value)
            ?? throw new KeyNotFoundException("Entity not found");

        return _mapper.Map<TDto>(entity);
    }

    protected virtual Expression<Func<TMedia, bool>> BuildFilterExpression(MediaFilter mediaFilter, string? userId = null)
    {
        Expression<Func<TMedia, bool>> filter = media => true;

        if (!string.IsNullOrWhiteSpace(mediaFilter.SearchString))
        {
            var search = mediaFilter.SearchString;
            filter = filter.And(media =>
                media.Name.Contains(search)
                || (media.Description != null && media.Description.Contains(search))
                || media.Genres.Any(genre => genre.Contains(search)));
        }

        if (mediaFilter.From != null)
        {
            var from = UtcDateTime.Normalize(mediaFilter.From);
            filter = filter.And(media => media.ReleaseDate >= from);
        }

        if (mediaFilter.To != null)
        {
            var to = UtcDateTime.Normalize(mediaFilter.To);
            filter = filter.And(media => media.ReleaseDate < to);
        }

        if (!string.IsNullOrWhiteSpace(mediaFilter.Genre))
        {
            var genre = mediaFilter.Genre;
            filter = filter.And(media => media.Genres.Contains(genre));
        }

        if (!string.IsNullOrWhiteSpace(mediaFilter.Source))
        {
            var source = mediaFilter.Source;
            filter = filter.And(media => media.Source == source);
        }

        if (mediaFilter.MyMedia && userId != null)
        {
            filter = filter.And(IsInUserLibrary(userId));
        }

        if (mediaFilter.MediaStatus != null && userId != null)
        {
            var mediaStatus = (int)mediaFilter.MediaStatus.Value;
            filter = filter.And(media => EF.Property<IEnumerable<TUserMedia>>(media, UserLibraryNavigationName)
                .Any(entry => entry.LuminaUserId == userId && EF.Property<int>(entry, "Status") == mediaStatus));
        }

        if (userId != null && string.Equals(mediaFilter.Ownership, "mine", StringComparison.OrdinalIgnoreCase))
        {
            filter = filter.And(IsInUserLibrary(userId));
        }

        if (userId != null && string.Equals(mediaFilter.Ownership, "catalog", StringComparison.OrdinalIgnoreCase))
        {
            filter = filter.And(IsNotInUserLibrary(userId));
        }

        return filter;
    }

    protected virtual IOrderedQueryable<TMedia> ApplyOrdering(IQueryable<TMedia> query, MediaFilter mediaFilter, string? userId)
    {
        return mediaFilter.SortBy?.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(media => media.Name),
            "release-desc" => query.OrderByDescending(media => media.ReleaseDate).ThenBy(media => media.Name),
            "release-asc" => query.OrderBy(media => media.ReleaseDate == null).ThenBy(media => media.ReleaseDate).ThenBy(media => media.Name),
            "rating-desc" when userId != null => query
                .OrderByDescending(media => EF.Property<IEnumerable<TUserMedia>>(media, UserLibraryNavigationName)
                    .Where(entry => entry.LuminaUserId == userId)
                    .Select(entry => entry.Rating)
                    .FirstOrDefault())
                .ThenBy(media => media.Name),
            "recently-added" when userId != null => query
                .OrderByDescending(media => EF.Property<IEnumerable<TUserMedia>>(media, UserLibraryNavigationName)
                    .Where(entry => entry.LuminaUserId == userId)
                    .Select(entry => entry.Id)
                    .FirstOrDefault())
                .ThenBy(media => media.Name),
            _ => DefaultOrderBy(query),
        };
    }

    private Expression<Func<TMedia, bool>> ComposeFilter(
        MediaFilter mediaFilter,
        string userId,
        Expression<Func<TMedia, bool>>? additionalFilter)
    {
        var filter = BuildFilterExpression(mediaFilter, userId);
        return additionalFilter is null ? filter : filter.And(additionalFilter);
    }

    protected virtual Expression<Func<TMedia, bool>> IsNotInUserLibrary(string userId)
    {
        var inLibrary = IsInUserLibrary(userId);
        return inLibrary.Not();
    }

    private IEnumerable<string> GetDefaultIncludes(IEnumerable<string>? includes)
    {
        return (includes ?? Includes)
            .Append(nameof(Media.Image))
            .Distinct();
    }

    private IQueryable<TMedia> IncludeNonLibraryNavigations(IQueryable<TMedia> query, IEnumerable<string>? includes)
    {
        return IncludeNavigations(
            query,
            includes?.Where(include =>
                !string.Equals(include, nameof(Media.Image), StringComparison.Ordinal)
                && !string.Equals(include, UserLibraryNavigationName, StringComparison.Ordinal)));
    }

    private static IQueryable<TMedia> IncludeNavigations(IQueryable<TMedia> query, IEnumerable<string>? includes)
    {
        return includes?
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Distinct()
            .Aggregate(query, (current, include) => current.Include(include))
            ?? query;
    }
}
