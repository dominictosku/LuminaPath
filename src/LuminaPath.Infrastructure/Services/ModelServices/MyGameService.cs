using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class MyGameService : GenericMyModelService<MyGame>
    {
        public MyGameService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper) : base(dbContextFactory, mapper)
        {
        }

        public override string[] Includes { get; set; } = ["Game"];
        protected override Func<IQueryable<MyGame>, IOrderedQueryable<MyGame>> DefaultOrderBy => e => e.OrderByDescending(g => g.Game!.ReleaseDate);

        public async Task<byte[]> ExportAsCSV(LuminaUser user)
        {
            using var context = await GetDbContextAsync();
            var myGames = await context.MyGames
                .Where(g => g.LuminaUserId == user.Id)
                .Include(g => g.MyGameInfo)
                .Include(g => g.Game)
                    .ThenInclude(g => g!.GameInfo)
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
                Map(m => m.Game!.GameInfo!.PsnId);
            }
        }
    }
}
