using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class MovieService : MediaModelService<Movie, MyMovie>
    {
        public override string[] Includes { get; set; } =
        [
            nameof(Movie.MyMovies),
            nameof(Movie.Image)
        ];
        protected override string UserLibraryNavigationName => nameof(Movie.MyMovies);

        public MovieService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            DocumentService documentService,
            IObjectMapper mapper,
            AuditLogService? auditLog = null)
            : base(dbContextFactory, documentService, mapper, auditLog)
        {
        }

        protected override IQueryable<Movie> IncludeUserLibrary(IQueryable<Movie> query, string userId)
        {
            return query.Include(movie => movie.MyMovies!.Where(myMovie => myMovie.LuminaUserId == userId));
        }

        protected override Expression<Func<Movie, bool>> IsInUserLibrary(string userId)
        {
            return movie => movie.MyMovies != null && movie.MyMovies.Any(myMovie => myMovie.LuminaUserId == userId);
        }

        public Task<List<Movie>> GetDropdownMovies(string? searchName = null)
        {
            return GetDropdownMedia(searchName);
        }
    }
}
