using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class AnimeConfiguration : IEntityTypeConfiguration<Anime>
    {
        public void Configure(EntityTypeBuilder<Anime> builder)
        {
            builder.HasOne(anime => anime.ParentAnime)
                .WithMany(anime => anime.Seasons)
                .HasForeignKey(anime => anime.ParentAnimeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(anime => anime.ParentAnimeId);
        }
    }
}
