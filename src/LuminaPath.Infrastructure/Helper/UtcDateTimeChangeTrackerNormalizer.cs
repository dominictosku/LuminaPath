using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LuminaPath.Infrastructure.Helper
{
    public static class UtcDateTimeChangeTrackerNormalizer
    {
        public static void Normalize(ChangeTracker changeTracker)
        {
            foreach (var entry in changeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                NormalizeDateTimeProperties(entry);
            }
        }

        private static void NormalizeDateTimeProperties(EntityEntry entry)
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime)
                {
                    SetNormalizedValue(entry, property, UtcDateTime.Normalize(dateTime));
                }
                else if (property.CurrentValue is DateTimeOffset dateTimeOffset)
                {
                    SetNormalizedValue(entry, property, UtcDateTime.Normalize(dateTimeOffset));
                }
            }
        }

        private static void SetNormalizedValue(EntityEntry entry, PropertyEntry property, object value)
        {
            property.CurrentValue = value;
            property.Metadata.PropertyInfo?.SetValue(entry.Entity, value);
        }
    }
}
