using AutoMapper;
using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Common.Entities.Results;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services
{
    public class MyGameService : GenericModelService<MyGame>
    {
        public MyGameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IMapper mapper) : base(dbContextFactory, mapper)
        {
        }

        public async Task<List<MyGame>> GetMyGames(string UserId, Expression<Func<MyGame, bool>> filter = null)
        {
            using var context = await GetDbContextAsync();
            IQueryable<MyGame> entities = GetEntities(context);
            entities = entities.Where(g => g.LuminaUserId == UserId).Include(g => g.Game);
            entities = PrepareEntity(entities, filter, e => e.OrderByDescending(g => g.Game.ReleaseDate));
            return await entities.ToListAsync();
        }

        public async Task<PaginatedList<MyGame>> GetAllPaginated(
            string UserId,
            MediaFilter mediaFilter,
            Expression<Func<MyGame, bool>> filter = null,
            Func<IQueryable<MyGame>, IOrderedQueryable<MyGame>> orderBy = null)
        {
            if (orderBy == null)
                orderBy = e => e.OrderBy(g => g.Game.ReleaseDate);
            Expression<Func<MyGame, bool>> userFilter = g => g.LuminaUserId == UserId;
            return await base.GetAllPaginated(mediaFilter, [ "Game" ], userFilter, orderBy);
        }

        public async Task<Result<MyGame, FailedResult>> PostAsync(MyGame viewModel, LuminaUser? user)
        {
            if (user == null)
                return new FailedResult("User not found, please login");
            if (await IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
            {
                return new FailedResult("This is game already added");
            }
            viewModel.LuminaUserId = user.Id;

            return await base.PostAsync(viewModel);
        }

        public async Task<Result<MyGame, FailedResult>> PutAsync(MyGame viewModel, LuminaUser? user)
        {
            if (user == null)
                return new FailedResult("User not found, please login");
            if (await IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
            {
                return new FailedResult("This is game already added");
            }
            viewModel.LuminaUserId = user.Id;
            return await base.PutAsync(viewModel);
        }

        public async Task DeleteMyData(LuminaUser user)
        {
            using var context = await GetDbContextAsync();
            var myGames = context.MyGames.Where(g => g.LuminaUserId == user.Id);
            context.MyGames.RemoveRange(myGames);
            await context.SaveChangesAsync();
        }

        protected async Task<bool> IsMediaAlreadyAdded(int id, int myId, string userId)
        {
            using var context = await GetDbContextAsync();
            var entities = GetEntities(context);
            var result = entities.AsNoTracking().ToList();
            return result.Any(e => e.MediaId == id && e.Id != myId && e.LuminaUserId == userId);
        }
    }
}
