using LuminaPath.Core.Mapping;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
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

        public AnimeService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, DocumentService documentService, IObjectMapper mapper)
            : base(dbContextFactory, documentService, mapper)
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

        public Task<List<Anime>> GetDropdownAnimes(string? searchName = null)
        {
            return GetDropdownMedia(searchName);
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
