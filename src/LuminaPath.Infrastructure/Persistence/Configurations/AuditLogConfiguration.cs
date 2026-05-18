using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(log => log.Category)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(log => log.Action)
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(log => log.Outcome)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(log => log.ActorUserId)
            .HasMaxLength(128);

        builder.Property(log => log.ActorEmail)
            .HasMaxLength(256);

        builder.Property(log => log.TargetType)
            .HasMaxLength(128);

        builder.Property(log => log.TargetId)
            .HasMaxLength(128);

        builder.Property(log => log.TargetName)
            .HasMaxLength(256);

        builder.Property(log => log.RequestPath)
            .HasMaxLength(512);

        builder.Property(log => log.HttpMethod)
            .HasMaxLength(16);

        builder.Property(log => log.IpAddress)
            .HasMaxLength(64);

        builder.Property(log => log.UserAgent)
            .HasMaxLength(512);

        builder.Property(log => log.CorrelationId)
            .HasMaxLength(128);

        builder.Property(log => log.ErrorMessage)
            .HasMaxLength(2048);

        builder.HasIndex(log => log.TimestampUtc);
        builder.HasIndex(log => log.Category);
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => log.Outcome);
        builder.HasIndex(log => log.ActorUserId);
        builder.HasIndex(log => new { log.TargetType, log.TargetId });
    }
}
