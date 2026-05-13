using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class SeriesConfiguration : IEntityTypeConfiguration<Series>
    {
        public void Configure(EntityTypeBuilder<Series> builder)
        {
            builder.HasOne(series => series.ParentSeries)
                .WithMany(series => series.Seasons)
                .HasForeignKey(series => series.ParentSeriesId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(series => series.ParentSeriesId);
        }
    }
}
