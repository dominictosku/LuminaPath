using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestConfiguration : IEntityTypeConfiguration<Quest>
    {
        public void Configure(EntityTypeBuilder<Quest> builder)
        {
            builder.HasOne(quest => quest.MyGame)
                .WithMany(myGame => myGame.Quests)
                .HasForeignKey(quest => quest.MyGameId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(quest => quest.Skill)
                .WithMany()
                .HasForeignKey(quest => quest.SkillId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(quest => quest.Subtasks)
                .WithOne(subtask => subtask.Quest!)
                .HasForeignKey(subtask => subtask.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.PrimitiveCollection(quest => quest.Tags)
                .HasColumnType("text[]");

            builder.HasIndex(quest => new { quest.LuminaUserId, quest.Completed, quest.DueDate });
            builder.HasIndex(quest => new { quest.LuminaUserId, quest.SortOrder });
        }
    }
}
