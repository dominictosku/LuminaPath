using LuminaPath.Core.Entities;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class GameService : MediaModelService<Game, MyGame>
    {
        public override string[] Includes { get; set; } = [nameof(Game.GameInfo), nameof(Game.MyGames), nameof(Game.Image)];
        protected override string UserLibraryNavigationName => nameof(Game.MyGames);

        public GameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, DocumentService documentService, IObjectMapper mapper)
            : base(dbContextFactory, documentService, mapper)
        {
        }

        protected override IQueryable<Game> IncludeUserLibrary(IQueryable<Game> query, string userId)
        {
            return query.Include(g => g.MyGames!.Where(p => p.LuminaUserId == userId));
        }

        protected override Expression<Func<Game, bool>> IsInUserLibrary(string userId)
        {
            return game => game.MyGames != null && game.MyGames.Any(myGame => myGame.LuminaUserId == userId);
        }

        protected override Expression<Func<Game, bool>> BuildFilterExpression(MediaFilter mediaFilter, string? userId = null)
        {
            var filter = base.BuildFilterExpression(mediaFilter, userId);

            if (mediaFilter.Platform != null)
            {
                var platform = mediaFilter.Platform.Value;
                filter = filter.And(game => game.Platforms.HasFlag(platform));
            }

            if (mediaFilter.MinPlaytime != null)
            {
                var minPlaytime = mediaFilter.MinPlaytime.Value;
                filter = filter.And(game => game.Playtime >= minPlaytime);
            }

            if (mediaFilter.MaxPlaytime != null)
            {
                var maxPlaytime = mediaFilter.MaxPlaytime.Value;
                filter = filter.And(game => game.Playtime <= maxPlaytime);
            }

            return filter;
        }

        public Task<List<Game>> GetDropdownGames(string? searchName = null)
        {
            return GetDropdownMedia(searchName);
        }
    }
}
