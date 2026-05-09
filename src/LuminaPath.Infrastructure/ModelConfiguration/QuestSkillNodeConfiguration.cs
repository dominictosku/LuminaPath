using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestSkillNodeConfiguration : IEntityTypeConfiguration<QuestSkillNode>
    {
        public void Configure(EntityTypeBuilder<QuestSkillNode> builder)
        {
            builder.HasOne(node => node.QuestSkill)
                .WithMany(skill => skill.Nodes)
                .HasForeignKey(node => node.QuestSkillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
