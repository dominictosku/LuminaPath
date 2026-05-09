using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestSkillConfiguration : IEntityTypeConfiguration<QuestSkill>
    {
        public void Configure(EntityTypeBuilder<QuestSkill> builder)
        {
            builder.HasOne(skill => skill.LuminaUser)
                .WithMany()
                .HasForeignKey(skill => skill.LuminaUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
