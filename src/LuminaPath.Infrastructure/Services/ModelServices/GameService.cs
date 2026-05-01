using AutoMapper;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class GameService : GenericModelService<Game>
    {
        private readonly DocumentService _documentService;
        public override string[] Includes { get; set; } = [nameof(Game.GameInfo), nameof(Game.MyGames), nameof(Game.Image)];
        public GameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, DocumentService documentService, IMapper mapper) : base(dbContextFactory, mapper)
        {
            _documentService = documentService;
        }

        public override async Task<Result<int, FailedResult>> DeleteAsync(int? id)
        {
            using var context = await GetDbContextAsync();
            var existing = await context.Games.Include(x => x.Image).FirstAsync(x => x.Id == id);
            await _documentService.DeleteMediaDocument(existing, context);
            return await base.DeleteAsync(id);
        }

        public async Task<List<Game>> GetDropdownGames(string? searchName = null)
        {
            using var context = await GetDbContextAsync();
            IQueryable<Game> query = context.Games;
            //if (searchName is not null) Todo! Adapt to Postgres
            //    query = query.Where(u => LuminaPathDbContext.pg_trgm(u.Name, searchName) > 0.3).OrderByDescending(u => LuminaPathDbContext.pg_trgm(u.Name, searchName));
            return await query.ToListAsync();
        }

        public async Task<PaginatedList<Game>> GetAllPaginated(
            MediaFilter mediaFilter,
            string UserId,
            Expression<Func<Game, bool>>? filter = null,
            IEnumerable<string>? includes = null)
        {
            using var context = await GetDbContextAsync();
            IQueryable<Game> entities = GetEntities(context);
            entities = entities.Include(g => g.Image).Include(g => g.MyGames!.Where(p => p.LuminaUserId == UserId));
            entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.ReleaseDate), includes);
            return await CreatePaginatedList(entities, mediaFilter.Paging);
        }

        public async Task<PaginatedList<TDto>> GetAndMapEntities<TDto>(MediaFilter mediaFilter, IEnumerable<string> includes, string? userId)
        {
            Expression<Func<Game, bool>> filter = GetFilterExpression(mediaFilter, userId);

            PaginatedList<Game> entities = userId != null
                ? await GetAllPaginated(mediaFilter, userId, filter)
                : await GetAllPaginated(mediaFilter, includes, filter);

            var entitiesDto = _mapper.Map<IEnumerable<Game>, IEnumerable<TDto>>(entities);
            return CreatePaginatedList(entitiesDto, mediaFilter.Paging);
        }

        private static Expression<Func<Game, bool>> GetFilterExpression(MediaFilter mediaFilter, string? userId = null)
        {
            Expression<Func<Game, bool>> filter = g => true;

            if (mediaFilter.SearchString != null)
            {
                filter = g => g.Name.Contains(mediaFilter.SearchString);
            }

            if (mediaFilter.From != null)
            {
                filter = filter.And(g => g.ReleaseDate > mediaFilter.From);
            }

            if (mediaFilter.To != null)
            {
                filter = filter.And(g => g.ReleaseDate < mediaFilter.To);
            }

            if (mediaFilter.MyMedia && userId != null)
            {
                filter = filter.And(g => g.MyGames != null && g.MyGames.Any(m => m.LuminaUserId == userId));
            }

            return filter;
        }
    }
}
