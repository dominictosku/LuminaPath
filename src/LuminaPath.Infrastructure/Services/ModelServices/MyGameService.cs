using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class MyGameService : UserMediaModelService<MyGame, Game>
    {
        public MyGameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper) : base(dbContextFactory, mapper)
        {
        }

        public override string[] Includes { get; set; } =
        [
            nameof(MyGame.Game),
            $"{nameof(MyGame.Game)}.{nameof(Game.Image)}",
            $"{nameof(MyGame.Game)}.{nameof(Game.ExternalIds)}",
            nameof(MyGame.MyGameInfo)
        ];
        protected override Func<IQueryable<MyGame>, IOrderedQueryable<MyGame>> DefaultOrderBy => e => e.OrderByDescending(g => g.Game!.ReleaseDate);

        protected override Expression<Func<MyGame, bool>> HasMediaId(int mediaId)
        {
            return myGame => myGame.GameId == mediaId;
        }

        public async Task<List<UserGameAchievementDto>?> GetEarnedAchievementsAsync(int myGameId, string userId)
        {
            using var context = await GetDbContextAsync();
            var gameId = await context.MyGames
                .AsNoTracking()
                .Where(myGame => myGame.Id == myGameId && myGame.LuminaUserId == userId)
                .Select(myGame => (int?)myGame.GameId)
                .SingleOrDefaultAsync();

            if (gameId is null)
            {
                return null;
            }

            var achievements = await (
                    from unlock in context.UserGameAchievements.AsNoTracking()
                    join achievement in context.GameAchievements.AsNoTracking()
                        on unlock.GameAchievementId equals achievement.Id
                    where unlock.LuminaUserId == userId && achievement.GameId == gameId.Value
                    orderby unlock.UnlockedAt ?? unlock.SyncedAt descending, achievement.Title
                    select new UserGameAchievementDto
                    {
                        Id = unlock.Id,
                        GameAchievementId = achievement.Id,
                        Provider = unlock.Provider,
                        SourceAchievementId = unlock.SourceAchievementId,
                        Title = achievement.Title,
                        Description = achievement.Description,
                        IconUrl = achievement.IconUrl,
                        IsHidden = achievement.IsHidden,
                        TrophyType = achievement.PsnTrophyType,
                        UnlockedAt = unlock.UnlockedAt,
                        SyncedAt = unlock.SyncedAt
                    })
                .ToListAsync();

            foreach (var achievement in achievements)
            {
                achievement.ProviderName = ProviderLabel(achievement.Provider);
            }

            return achievements;
        }

        private static string ProviderLabel(ExternalMediaProvider provider)
        {
            return provider switch
            {
                ExternalMediaProvider.Psn => "PlayStation",
                _ => provider.ToString()
            };
        }

        public async Task<byte[]> ExportAsCSV(LuminaUser user)
        {
            using var context = await GetDbContextAsync();
            var myGames = await context.MyGames
                .Where(g => g.LuminaUserId == user.Id)
                .Include(g => g.MyGameInfo)
                .Include(g => g.Game)
                    .ThenInclude(g => g!.ExternalIds)
                .OrderBy(g => g.Game!.Name)
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var options = new TypeConverterOptions { Formats = new[] { "dd.MM.yyyy HH:mm" } };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.Context.RegisterClassMap<MyGameMap>();
                csv.WriteRecords(myGames);
                writer.Flush();
            }

            return memoryStream.ToArray();
        }

        public sealed class MyGameMap : ClassMap<MyGame>
        {
            public MyGameMap()
            {
                Map(m => m.Game!.Id);
                Map(m => m.Game!.Name);
                Map(m => m.Status);
                Map(m => m.Game!.ReleaseDate);
                Map(m => m.Game!.Platforms);
                Map(m => m.Game!.Source);
                Map(m => m.MyGameInfo!.FirstPlayed);
                Map(m => m.MyGameInfo!.LastPlayed);
                Map(m => m.MyGameInfo!.TrackedHours);
                Map(m => m.Game)
                    .Name("PsnId")
                    .Convert((ConvertToString<MyGame>)(args => args.Value.Game?.ExternalIds.GetExternalId(ExternalMediaProvider.Psn)));
            }
        }
    }
}
