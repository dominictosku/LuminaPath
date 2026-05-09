using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class MyAnimeConfiguration : IEntityTypeConfiguration<MyAnime>
    {
        public void Configure(EntityTypeBuilder<MyAnime> builder)
        {
            builder.HasOne(myAnime => myAnime.Anime)
                .WithMany(anime => anime.MyAnimes)
                .HasForeignKey(myAnime => myAnime.AnimeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
