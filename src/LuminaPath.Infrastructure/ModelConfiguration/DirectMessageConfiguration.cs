using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class DirectMessageConfiguration : IEntityTypeConfiguration<DirectMessage>
    {
        public void Configure(EntityTypeBuilder<DirectMessage> builder)
        {
            builder.HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(message => message.Recipient)
                .WithMany()
                .HasForeignKey(message => message.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(message => new { message.SenderId, message.RecipientId, message.SentAt });
        }
    }
}
