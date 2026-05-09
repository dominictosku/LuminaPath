using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class MyAnimeService : UserMediaModelService<MyAnime, Anime>
    {
        public override string[] Includes { get; set; } =
        [
            nameof(MyAnime.Anime),
            $"{nameof(MyAnime.Anime)}.{nameof(Anime.Image)}"
        ];

        protected override Func<IQueryable<MyAnime>, IOrderedQueryable<MyAnime>> DefaultOrderBy
            => myAnimes => myAnimes.OrderByDescending(myAnime => myAnime.Anime!.ReleaseDate);

        public MyAnimeService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper)
            : base(dbContextFactory, mapper)
        {
        }

        protected override Expression<Func<MyAnime, bool>> HasMediaId(int mediaId)
        {
            return myAnime => myAnime.AnimeId == mediaId;
        }
    }
}
