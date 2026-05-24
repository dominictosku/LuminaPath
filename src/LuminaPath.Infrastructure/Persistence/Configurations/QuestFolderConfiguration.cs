using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestFolderConfiguration : IEntityTypeConfiguration<QuestFolder>
    {
        public void Configure(EntityTypeBuilder<QuestFolder> builder)
        {
            builder.Property(folder => folder.Name).HasMaxLength(80).IsRequired();
            builder.Property(folder => folder.Emoji).HasMaxLength(16).IsRequired();
            builder.Property(folder => folder.Color).HasMaxLength(16);
            builder.Property(folder => folder.SectionName).HasMaxLength(48);

            // Sorted scans per user — folders list always filters by user
            // and orders by (SectionName, SortOrder), so this composite
            // index keeps the read query out of a full scan.
            builder.HasIndex(folder => new { folder.LuminaUserId, folder.SortOrder });
        }
    }
}
