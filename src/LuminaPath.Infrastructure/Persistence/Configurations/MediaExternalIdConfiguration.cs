using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration;

public class MediaExternalIdConfiguration : IEntityTypeConfiguration<MediaExternalId>
{
    public void Configure(EntityTypeBuilder<MediaExternalId> builder)
    {
        builder.HasIndex(externalId => new { externalId.Provider, externalId.ExternalId })
            .IsUnique();

        builder.Property(externalId => externalId.ExternalId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne(externalId => externalId.Media)
            .WithMany(media => media.ExternalIds)
            .HasForeignKey(externalId => externalId.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
