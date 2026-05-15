using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class MySeriesService : UserMediaModelService<MySeries, Series>
    {
        public override string[] Includes { get; set; } =
        [
            nameof(MySeries.Series),
            $"{nameof(MySeries.Series)}.{nameof(Series.Image)}"
        ];

        protected override Func<IQueryable<MySeries>, IOrderedQueryable<MySeries>> DefaultOrderBy
            => mySeries => mySeries.OrderByDescending(item => item.Series!.ReleaseDate);

        public MySeriesService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, IObjectMapper mapper)
            : base(dbContextFactory, mapper)
        {
        }

        protected override Expression<Func<MySeries, bool>> HasMediaId(int mediaId)
        {
            return mySeries => mySeries.SeriesId == mediaId;
        }

        protected override async Task PrepareForSave(MySeries viewModel, LuminaUser user)
        {
            await using var dbContext = await GetDbContextAsync();
            var series = viewModel.Series ?? await dbContext.Series
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == viewModel.SeriesId);

            viewModel.RecalculateWatchTime(series);
            viewModel.Series = null;
        }
    }
}
