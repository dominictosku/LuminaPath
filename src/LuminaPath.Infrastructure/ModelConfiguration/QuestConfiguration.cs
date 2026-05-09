using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestConfiguration : IEntityTypeConfiguration<Quest>
    {
        public void Configure(EntityTypeBuilder<Quest> builder)
        {
            builder.HasOne(quest => quest.LuminaUser)
                .WithMany()
                .HasForeignKey(quest => quest.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(quest => quest.MyGame)
                .WithMany(myGame => myGame.Quests)
                .HasForeignKey(quest => quest.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
