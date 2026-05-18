using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class SeriesService : MediaModelService<Series, MySeries>
    {
        public override string[] Includes { get; set; } =
        [
            nameof(Series.MySeries),
            nameof(Series.Image)
        ];

        protected override string UserLibraryNavigationName => nameof(Series.MySeries);

        public SeriesService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            DocumentService documentService,
            IObjectMapper mapper,
            AuditLogService? auditLog = null)
            : base(dbContextFactory, documentService, mapper, auditLog)
        {
        }

        protected override IQueryable<Series> IncludeUserLibrary(IQueryable<Series> query, string userId)
        {
            return query.Include(series => series.MySeries!.Where(mySeries => mySeries.LuminaUserId == userId));
        }

        protected override Expression<Func<Series, bool>> IsInUserLibrary(string userId)
        {
            return series => series.MySeries != null && series.MySeries.Any(mySeries => mySeries.LuminaUserId == userId);
        }

        protected override Expression<Func<Series, bool>> BuildFilterExpression(MediaFilter mediaFilter, string? userId = null)
        {
            var filter = base.BuildFilterExpression(mediaFilter, userId);

            if (!mediaFilter.IncludeChildren)
            {
                filter = filter.And(series => series.ParentSeriesId == null);
            }

            return filter;
        }

        public Task<List<Series>> GetDropdownSeries(string? searchName = null)
        {
            return GetDropdownMedia(searchName);
        }

        public async Task<List<Series>> GetDropdownParentSeries(string? searchName = null, int? excludeId = null)
        {
            await using var context = await GetDbContextAsync();
            IQueryable<Series> query = context.Series
                .Include(series => series.Image)
                .Where(series => series.ParentSeriesId == null);

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(series => series.Name.Contains(searchName));
            }

            if (excludeId is int id && id > 0)
            {
                query = query.Where(series => series.Id != id);
            }

            return await query.OrderBy(series => series.Name).ToListAsync();
        }

        public override Task<Result<Series, FailedResult>> PostAsync(Series entity)
        {
            RecalculateUserEntries(entity);
            return base.PostAsync(entity);
        }

        public override Task<Result<Series, FailedResult>> PutAsync(Series entity)
        {
            RecalculateUserEntries(entity);
            return base.PutAsync(entity);
        }

        public async Task<List<Series>> GetSeasonsAsync(int parentSeriesId, CancellationToken cancellationToken = default)
        {
            await using var context = await GetDbContextAsync();
            return await context.Series
                .AsNoTracking()
                .Include(series => series.Image)
                .Where(series => series.ParentSeriesId == parentSeriesId)
                .OrderBy(series => series.ReleaseDate)
                .ThenBy(series => series.Name)
                .ToListAsync(cancellationToken);
        }

        private static void RecalculateUserEntries(Series series)
        {
            if (series.MySeries is null)
            {
                return;
            }

            foreach (var mySeries in series.MySeries)
            {
                mySeries.RecalculateWatchTime(series);
            }
        }
    }
}
