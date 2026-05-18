using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration;

public class BackgroundJobRecordConfiguration : IEntityTypeConfiguration<BackgroundJobRecord>
{
    public void Configure(EntityTypeBuilder<BackgroundJobRecord> builder)
    {
        builder.Property(job => job.JobType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(job => job.DisplayName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(job => job.ResultMessage)
            .HasMaxLength(1024);

        builder.Property(job => job.ErrorMessage)
            .HasMaxLength(4000);

        builder.HasIndex(job => job.Status);
        builder.HasIndex(job => job.CreatedAt);
        builder.HasIndex(job => new { job.JobType, job.CreatedAt });
    }
}
