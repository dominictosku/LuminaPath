using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.HasIndex(game => game.Name).IsUnique();

            builder.HasOne(game => game.ParentGame)
                .WithMany(game => game.Dlcs)
                .HasForeignKey(game => game.ParentGameId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(game => game.ParentGameId);
        }
    }
}
