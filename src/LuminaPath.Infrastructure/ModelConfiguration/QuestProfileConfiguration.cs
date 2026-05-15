using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestProfileConfiguration : IEntityTypeConfiguration<QuestProfile>
    {
        public void Configure(EntityTypeBuilder<QuestProfile> builder)
        {
            builder.HasIndex(profile => profile.LuminaUserId)
                .IsUnique();
        }
    }
}
