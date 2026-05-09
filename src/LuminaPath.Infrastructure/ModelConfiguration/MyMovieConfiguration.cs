using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class MyMovieConfiguration : IEntityTypeConfiguration<MyMovie>
    {
        public void Configure(EntityTypeBuilder<MyMovie> builder)
        {
            builder.HasOne(myMovie => myMovie.Movie)
                .WithMany(movie => movie.MyMovies)
                .HasForeignKey(myMovie => myMovie.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
