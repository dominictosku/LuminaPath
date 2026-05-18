using LuminaPath.Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LuminaPath.Infrastructure.ModelConfiguration
{
    public static class UtcDateTimeModelConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                dateTime => UtcDateTime.Normalize(dateTime),
                dateTime => UtcDateTime.Normalize(dateTime));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                dateTime => UtcDateTime.Normalize(dateTime),
                dateTime => UtcDateTime.Normalize(dateTime));

            var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset),
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset));

            var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset),
                dateTimeOffset => UtcDateTime.Normalize(dateTimeOffset));

            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableDateTimeOffsetConverter);
                }
            }
        }
    }
}
