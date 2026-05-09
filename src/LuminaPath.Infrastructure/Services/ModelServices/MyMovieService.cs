using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class MyMovieService : UserMediaModelService<MyMovie, Movie>
    {
        public override string[] Includes { get; set; } =
        [
            nameof(MyMovie.Movie),
            $"{nameof(MyMovie.Movie)}.{nameof(Movie.Image)}"
        ];

        protected override Func<IQueryable<MyMovie>, IOrderedQueryable<MyMovie>> DefaultOrderBy
            => myMovies => myMovies.OrderByDescending(myMovie => myMovie.Movie!.ReleaseDate);

        public MyMovieService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper)
            : base(dbContextFactory, mapper)
        {
        }

        protected override Expression<Func<MyMovie, bool>> HasMediaId(int mediaId)
        {
            return myMovie => myMovie.MovieId == mediaId;
        }
    }
}
