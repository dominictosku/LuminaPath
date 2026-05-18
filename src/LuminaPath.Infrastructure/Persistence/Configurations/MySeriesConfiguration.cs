using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class MySeriesConfiguration : IEntityTypeConfiguration<MySeries>
    {
        public void Configure(EntityTypeBuilder<MySeries> builder)
        {
            builder.HasOne(mySeries => mySeries.Series)
                .WithMany(series => series.MySeries)
                .HasForeignKey(mySeries => mySeries.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
