using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Helper;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base;

public abstract class MediaModelService<TMedia, TUserMedia> : GenericModelService<TMedia>
    where TMedia : Media
    where TUserMedia : MyMedia, IMyMedia
{
    private readonly DocumentService _documentService;

    protected MediaModelService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        DocumentService documentService,
        IObjectMapper mapper)
        : base(dbContextFactory, mapper)
    {
        _documentService = documentService;
    }

    protected virtual Func<IQueryable<TMedia>, IOrderedQueryable<TMedia>> DefaultOrderBy
        => media => media
            .OrderByDescending(item => item.ReleaseDate)
            .ThenBy(item => item.Name);

    protected abstract IQueryable<TMedia> IncludeUserLibrary(IQueryable<TMedia> query, string userId);

    protected abstract Expression<Func<TMedia, bool>> IsInUserLibrary(string userId);

    protected abstract string UserLibraryNavigationName { get; }

    public override async Task<Result<TMedia, FailedResult>> PostAsync(TMedia entity)
    {
        var conflict = await EnsureNameUnique(entity);
        if (conflict is not null)
        {
            return conflict;
        }

        return await base.PostAsync(entity);
    }

    public override async Task<Result<TMedia, FailedResult>> PutAsync(TMedia entity)
    {
        var conflict = await EnsureNameUnique(entity);
        if (conflict is not null)
        {
            return conflict;
        }

        return await base.PutAsync(entity);
    }

    private async Task<ValidationFailed?> EnsureNameUnique(TMedia entity)
    {
        await using var context = await GetDbContextAsync();
        var exists = await context.Set<TMedia>()
            .AsNoTracking()
            .AnyAsync(media => media.Id != entity.Id && media.Name == entity.Name);

        return exists
            ? new ValidationFailed(new ValidationFailure(nameof(Media.Name), "Title is already registered"))
            : null;
    }

    public override async Task<Result<int, FailedResult>> DeleteAsync(int? id)
    {
        if (id is null)
        {
            return new FailedResult("Entry not found");
        }

        await using var context = await GetDbContextAsync();
        var existing = await context.Set<TMedia>()
            .Include(media => media.Image)
            .FirstOrDefaultAsync(media => media.Id == id.Value);

        if (existing is null)
        {
            return new FailedResult("Entry not found");
        }

        await _documentService.DeleteMediaDocument(existing, context);
        return await base.DeleteAsync(id);
    }

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
        entities = PrepareEntity(entities, composedFilter, DefaultOrderBy, includes);
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
            : await GetAllPaginated<TMedia>(mediaFilter, GetDefaultIncludes(includes), filter, DefaultOrderBy);

        var entitiesDto = _mapper.Map<IEnumerable<TMedia>, IEnumerable<TDto>>(entities);
        return CreatePaginatedList(entitiesDto, mediaFilter.Paging);
    }

    public virtual async Task<TDto> GetByIdAndMap<TDto>(int? id, IEnumerable<string> includes, string? userId)
    {
        if (id is null)
        {
            throw new Exception("No id given");
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
            filter = filter.And(media => media.Name.Contains(search));
        }

        if (mediaFilter.From != null)
        {
            var from = UtcDateTime.Normalize(mediaFilter.From);
            filter = filter.And(media => media.ReleaseDate > from);
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

        return filter;
    }

    private Expression<Func<TMedia, bool>> ComposeFilter(
        MediaFilter mediaFilter,
        string userId,
        Expression<Func<TMedia, bool>>? additionalFilter)
    {
        var filter = BuildFilterExpression(mediaFilter, userId);
        return additionalFilter is null ? filter : filter.And(additionalFilter);
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
