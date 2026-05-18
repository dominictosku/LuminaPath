using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class QuestSubtaskConfiguration : IEntityTypeConfiguration<QuestSubtask>
    {
        public void Configure(EntityTypeBuilder<QuestSubtask> builder)
        {
            builder.Property(subtask => subtask.Title).IsRequired();
            builder.HasIndex(subtask => new { subtask.QuestId, subtask.SortOrder });
        }
    }
}
