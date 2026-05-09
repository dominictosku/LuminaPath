using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class GamingSessionConfiguration : IEntityTypeConfiguration<GamingSession>
    {
        public void Configure(EntityTypeBuilder<GamingSession> builder)
        {
            builder.HasOne(session => session.LuminaUser)
                .WithMany()
                .HasForeignKey(session => session.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(session => session.MyGame)
                .WithMany(myGame => myGame.GamingSessions)
                .HasForeignKey(session => session.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(session => new { session.LuminaUserId, session.ScheduledAt });
        }
    }
}
