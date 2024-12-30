using AutoMapper;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Common.Extensions;
using LuminaPath.Core.Common.Features.Gaming.Dto;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services
{
    public class GameService : GenericModelService<Game>
    {
        private readonly DocumentService _documentService;
        public GameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, DocumentService documentService, IMapper mapper) : base(dbContextFactory, mapper)
        {
            _documentService = documentService;
        }

        public async Task ImportGames(LuminaUser user, List<MyGameDto> games)
        {
            using var context = await GetDbContextAsync();
            List<Game> gamesToAdd = new();
            foreach (var game in games) 
            {
                if(!context.Games.Any(g => g.GameInfo != null && g.GameInfo.PsnId == game.Game.GameInfo.PsnId))
                {                    
                    var newGame = new Game()
                    {
                        Name = game.Game.Name,
                        Plattforms = game.Game.Plattforms,
                        Source = "PSN",
                        GameInfo = new()
                        {
                            PsnId = game.Game.GameInfo.PsnId
                        },
                        MyGames = [ new MyGame() {
                            LuminaUserId = user.Id
                        }]
                    };
                    if(gamesToAdd.Any(g => g.Name == newGame.Name))
                        newGame.Name = newGame.Name + " Duplicate " + game.Game.GameInfo.PsnId;
                    gamesToAdd.Add(newGame);
                }
                else
                {
                    var existingGame = context.Games.Where(g => g.GameInfo.PsnId == game.Game.GameInfo.PsnId).First();
                    if (existingGame.MyGames.Any(g => g.LuminaUserId == user.Id))
                    {
                        var myGame = existingGame.MyGames.First(g => g.LuminaUserId == user.Id);
                        myGame.MyGameInfo = game.MyGameInfo;
                    }
                    else
                    {
                        context.MyGames.Add(new MyGame() { GameId = existingGame.Id, LuminaUserId = user.Id, MyGameInfo = game.MyGameInfo });
                    }
                }
            }
            await context.Games.AddRangeAsync(gamesToAdd);
            await context.SaveChangesAsync();
        }

        public override async Task<Result<int, FailedResult>> DeleteAsync(int? id)
        {
            using var context = await GetDbContextAsync();
            var existing = await context.Games.Include(x => x.Image).FirstAsync(x => x.Id == id);
            await _documentService.DeleteMediaDocument(existing, context);
            return await base.DeleteAsync(id);
        }

        public async Task<List<Game>> GetDropdownGames()
        {
            using var context = await GetDbContextAsync();
            return context.Games.ToList();
        }

        public async Task<List<Game>> GetDropdownGames(string searchName)
        {
            using var context = await GetDbContextAsync();
            return context.Games.Where(x => LuminaPathDbContext.Soundex(x.Name) == LuminaPathDbContext.Soundex(searchName)).ToList();
        }

        public async Task<PaginatedList<Game>> GetAllPaginated(
            MediaFilter mediaFilter,
            string UserId,
            Expression<Func<Game, bool>> filter = null,
            IEnumerable<string> includes = null)
        {
            using var context = await GetDbContextAsync();
            IQueryable<Game> entities = GetEntities(context);
            entities = entities.Include(g => g.Image).Include(g => g.MyGames.Where(p => p.LuminaUserId == UserId));
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
