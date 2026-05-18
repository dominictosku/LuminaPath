using LuminaPath.Core.Entities;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class GameService : MediaModelService<Game, MyGame>
    {
        public override string[] Includes { get; set; } = [nameof(Game.ExternalIds), nameof(Game.MyGames), nameof(Game.Image)];
        protected override string UserLibraryNavigationName => nameof(Game.MyGames);

        public GameService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            DocumentService documentService,
            IObjectMapper mapper,
            AuditLogService? auditLog = null)
            : base(dbContextFactory, documentService, mapper, auditLog)
        {
        }

        protected override IQueryable<Game> IncludeUserLibrary(IQueryable<Game> query, string userId)
        {
            return query
                .Include(g => g.MyGames!.Where(p => p.LuminaUserId == userId))
                    .ThenInclude(myGame => myGame.MyGameInfo);
        }

        protected override Expression<Func<Game, bool>> IsInUserLibrary(string userId)
        {
            return game => game.MyGames != null && game.MyGames.Any(myGame => myGame.LuminaUserId == userId);
        }

        protected override Expression<Func<Game, bool>> BuildFilterExpression(MediaFilter mediaFilter, string? userId = null)
        {
            var filter = base.BuildFilterExpression(mediaFilter, userId);

            if (!mediaFilter.IncludeChildren)
            {
                filter = filter.And(game => game.ParentGameId == null);
            }

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

            if (mediaFilter.Status != null && userId != null)
            {
                var status = mediaFilter.Status.Value;
                filter = filter.And(game => game.MyGames != null
                    && game.MyGames.Any(myGame => myGame.LuminaUserId == userId && myGame.Status == status));
            }

            if (userId != null)
            {
                filter = mediaFilter.SmartFilter?.ToLowerInvariant() switch
                {
                    "short" => filter.And(game => game.MyGames != null
                        && game.MyGames.Any(myGame =>
                            myGame.LuminaUserId == userId
                            && myGame.Status != Core.Enums.GameStatus.Completed
                            && game.Playtime != null
                            && game.Playtime - ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)) > 0
                            && game.Playtime - ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)) <= 10)),
                    "abandoned" => filter.And(game => game.MyGames != null
                        && game.MyGames.Any(myGame =>
                            myGame.LuminaUserId == userId
                            && myGame.Status != Core.Enums.GameStatus.Playing
                            && myGame.Status != Core.Enums.GameStatus.Completed
                            && ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)) > 0
                            && game.Playtime != null
                            && game.Playtime - ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)) > 0)),
                    "best" => filter.And(game => game.MyGames != null
                        && game.MyGames.Any(myGame =>
                            myGame.LuminaUserId == userId
                            && myGame.Status != Core.Enums.GameStatus.Completed
                            && game.Playtime != null
                            && game.Playtime - ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)) > 0)),
                    _ => filter
                };
            }

            return filter;
        }

        protected override IOrderedQueryable<Game> ApplyOrdering(IQueryable<Game> query, MediaFilter mediaFilter, string? userId)
        {
            if (userId is null)
            {
                return base.ApplyOrdering(query, mediaFilter, userId);
            }

            return mediaFilter.SortBy?.ToLowerInvariant() switch
            {
                "remaining-asc" => query
                    .OrderBy(game => game.MyGames!
                        .Where(myGame => myGame.LuminaUserId == userId)
                        .Select(myGame => (game.Playtime ?? 0) - ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)))
                        .FirstOrDefault())
                    .ThenBy(game => game.Name),
                "best-finish" => query
                    .OrderBy(game => game.MyGames!
                        .Where(myGame => myGame.LuminaUserId == userId)
                        .Select(myGame =>
                            ((game.Playtime ?? 0) - ((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)))
                            - (myGame.Status == Core.Enums.GameStatus.Playing ? 5 : 0)
                            - (((myGame.TimeSpend ?? 0) + (myGame.MyGameInfo != null ? myGame.MyGameInfo.TrackedHours : 0)) > 0 ? 3 : 0)
                            - ((myGame.Rating ?? 0) / 3.0))
                        .FirstOrDefault())
                    .ThenBy(game => game.Name),
                _ => base.ApplyOrdering(query, mediaFilter, userId),
            };
        }

        public Task<List<Game>> GetDropdownGames(string? searchName = null)
        {
            return GetDropdownMedia(searchName);
        }

        public Task<List<Game>> GetDropdownParentGames(string? searchName = null, int? excludeId = null)
        {
            return GetDropdownParentMedia(game => game.ParentGameId == null, searchName, excludeId);
        }

        public Task<List<Game>> GetDlcsAsync(int parentGameId, CancellationToken cancellationToken = default)
        {
            return GetChildMediaAsync(game => game.ParentGameId == parentGameId, cancellationToken, newestFirst: true);
        }
    }
}
