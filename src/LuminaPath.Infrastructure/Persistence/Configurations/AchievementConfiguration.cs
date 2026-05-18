using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
    {
        public void Configure(EntityTypeBuilder<Achievement> builder)
        {
            builder.Property(achievement => achievement.Code).IsRequired().HasMaxLength(64);

            builder.HasOne(achievement => achievement.QuestProfile)
                .WithMany(profile => profile.Achievements)
                .HasForeignKey(achievement => achievement.QuestProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(achievement => new { achievement.QuestProfileId, achievement.Code }).IsUnique();
        }
    }
}
