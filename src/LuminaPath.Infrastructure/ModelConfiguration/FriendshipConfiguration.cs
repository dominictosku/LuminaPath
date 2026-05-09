using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            builder.HasOne(friendship => friendship.Requester)
                .WithMany()
                .HasForeignKey(friendship => friendship.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(friendship => friendship.Addressee)
                .WithMany()
                .HasForeignKey(friendship => friendship.AddresseeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(friendship => new { friendship.RequesterId, friendship.AddresseeId })
                .IsUnique();
        }
    }
}
