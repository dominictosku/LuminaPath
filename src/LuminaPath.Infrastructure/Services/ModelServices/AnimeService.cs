using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class AnimeService : MediaModelService<Anime, MyAnime>
    {
        public override string[] Includes { get; set; } =
        [
            nameof(Anime.MyAnimes),
            nameof(Anime.Image)
        ];
        protected override string UserLibraryNavigationName => nameof(Anime.MyAnimes);

        public AnimeService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            DocumentService documentService,
            IObjectMapper mapper,
            AuditLogService? auditLog = null)
            : base(dbContextFactory, documentService, mapper, auditLog)
        {
        }

        protected override IQueryable<Anime> IncludeUserLibrary(IQueryable<Anime> query, string userId)
        {
            return query.Include(anime => anime.MyAnimes!.Where(myAnime => myAnime.LuminaUserId == userId));
        }

        protected override Expression<Func<Anime, bool>> IsInUserLibrary(string userId)
        {
            return anime => anime.MyAnimes != null && anime.MyAnimes.Any(myAnime => myAnime.LuminaUserId == userId);
        }

        protected override Expression<Func<Anime, bool>> BuildFilterExpression(MediaFilter mediaFilter, string? userId = null)
        {
            var filter = base.BuildFilterExpression(mediaFilter, userId);

            if (!mediaFilter.IncludeChildren)
            {
                filter = filter.And(anime => anime.ParentAnimeId == null);
            }

            return filter;
        }

        public Task<List<Anime>> GetDropdownAnimes(string? searchName = null)
        {
            return GetDropdownMedia(searchName);
        }

        public async Task<List<Anime>> GetDropdownParentAnimes(string? searchName = null, int? excludeId = null)
        {
            await using var context = await GetDbContextAsync();
            IQueryable<Anime> query = context.Animes
                .Include(anime => anime.Image)
                .Where(anime => anime.ParentAnimeId == null);

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(anime => anime.Name.Contains(searchName));
            }

            if (excludeId is int id && id > 0)
            {
                query = query.Where(anime => anime.Id != id);
            }

            return await query.OrderBy(anime => anime.Name).ToListAsync();
        }

        public override Task<Result<Anime, FailedResult>> PostAsync(Anime entity)
        {
            RecalculateUserEntries(entity);
            return base.PostAsync(entity);
        }

        public override Task<Result<Anime, FailedResult>> PutAsync(Anime entity)
        {
            RecalculateUserEntries(entity);
            return base.PutAsync(entity);
        }

        public async Task<List<Anime>> GetSeasonsAsync(int parentAnimeId, CancellationToken cancellationToken = default)
        {
            await using var context = await GetDbContextAsync();
            return await context.Animes
                .AsNoTracking()
                .Include(anime => anime.Image)
                .Where(anime => anime.ParentAnimeId == parentAnimeId)
                .OrderBy(anime => anime.ReleaseDate)
                .ThenBy(anime => anime.Name)
                .ToListAsync(cancellationToken);
        }

        private static void RecalculateUserEntries(Anime anime)
        {
            if (anime.MyAnimes is null)
            {
                return;
            }

            foreach (var myAnime in anime.MyAnimes)
            {
                myAnime.RecalculateWatchTime(anime);
            }
        }
    }
}
