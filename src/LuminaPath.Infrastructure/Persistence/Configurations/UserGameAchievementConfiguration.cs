using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration;

public class UserGameAchievementConfiguration : IEntityTypeConfiguration<UserGameAchievement>
{
    public void Configure(EntityTypeBuilder<UserGameAchievement> builder)
    {
        builder.Property(achievement => achievement.LuminaUserId)
            .IsRequired();

        builder.Property(achievement => achievement.SourceAchievementId)
            .HasMaxLength(180)
            .IsRequired();

        builder.HasOne(achievement => achievement.GameAchievement)
            .WithMany(achievement => achievement.UserAchievements)
            .HasForeignKey(achievement => achievement.GameAchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(achievement => new
            {
                achievement.LuminaUserId,
                achievement.GameAchievementId,
                achievement.Provider,
                achievement.SourceAchievementId
            })
            .IsUnique();

        builder.HasIndex(achievement => new { achievement.LuminaUserId, achievement.GameAchievementId });
    }
}
