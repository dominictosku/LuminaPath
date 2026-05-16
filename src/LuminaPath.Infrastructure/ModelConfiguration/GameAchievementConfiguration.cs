using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration;

public class GameAchievementConfiguration : IEntityTypeConfiguration<GameAchievement>
{
    public void Configure(EntityTypeBuilder<GameAchievement> builder)
    {
        builder.Property(achievement => achievement.CanonicalKey)
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(achievement => achievement.Title)
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(achievement => achievement.Description)
            .HasMaxLength(1200);

        builder.Property(achievement => achievement.IconUrl)
            .HasMaxLength(800);

        builder.Property(achievement => achievement.SteamApiName)
            .HasMaxLength(180);

        builder.Property(achievement => achievement.SteamDisplayName)
            .HasMaxLength(240);

        builder.Property(achievement => achievement.PsnGroupId)
            .HasMaxLength(80);

        builder.Property(achievement => achievement.PsnTrophyType)
            .HasMaxLength(40);

        builder.HasOne(achievement => achievement.Game)
            .WithMany(game => game.Achievements)
            .HasForeignKey(achievement => achievement.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(achievement => new { achievement.GameId, achievement.CanonicalKey })
            .IsUnique();

        builder.HasIndex(achievement => achievement.SteamApiName);
        builder.HasIndex(achievement => new { achievement.GameId, achievement.PsnTrophyId, achievement.PsnGroupId });
    }
}
