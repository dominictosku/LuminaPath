using LuminaPath.Core.Models.ThirdParty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public class CalendarIntegrationConfiguration : IEntityTypeConfiguration<CalendarIntegration>
    {
        public void Configure(EntityTypeBuilder<CalendarIntegration> builder)
        {
            // One link per user per provider.
            builder.HasIndex(integration => new { integration.LuminaUserId, integration.Provider })
                .IsUnique();

            builder.Property(integration => integration.Provider).HasMaxLength(32);
            builder.Property(integration => integration.AccountEmail).HasMaxLength(320);
        }
    }
}
