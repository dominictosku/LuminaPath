using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration;

public class MediaVideoConfiguration : IEntityTypeConfiguration<MediaVideo>
{
    public void Configure(EntityTypeBuilder<MediaVideo> builder)
    {
        builder.Property(video => video.Title)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(video => video.Description)
            .HasMaxLength(500);

        builder.Property(video => video.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(video => video.StorageName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(video => video.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(video => new { video.MediaId, video.SortOrder });
        builder.HasIndex(video => video.StorageName)
            .IsUnique();

        builder.HasOne(video => video.Media)
            .WithMany(media => media.Videos)
            .HasForeignKey(video => video.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
