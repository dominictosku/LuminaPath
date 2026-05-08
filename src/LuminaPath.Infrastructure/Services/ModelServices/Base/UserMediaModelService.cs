using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base;

public abstract class UserMediaModelService<TUserMedia, TMedia> : GenericMyModelService<TUserMedia>
    where TUserMedia : MyMedia, IMyMedia
    where TMedia : Media
{
    protected UserMediaModelService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper)
        : base(dbContextFactory, mapper)
    {
    }

    protected virtual string MediaDisplayName => typeof(TMedia).Name.ToLowerInvariant();

    protected override string DuplicateMediaMessage => $"This {MediaDisplayName} is already added";

    protected abstract Expression<Func<TUserMedia, bool>> HasMediaId(int mediaId);

    protected override async Task<bool> IsMediaAlreadyAdded(int mediaId, int userMediaId, string userId)
    {
        await using var context = await GetDbContextAsync();
        return await GetEntities(context)
            .AsNoTracking()
            .Where(item => item.Id != userMediaId && item.LuminaUserId == userId)
            .AnyAsync(HasMediaId(mediaId));
    }
}
