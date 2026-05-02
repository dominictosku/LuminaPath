namespace LuminaPath.Infrastructure.Helper
{
    public static class UtcDateTime
    {
        public static DateTime Normalize(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };
        }

        public static DateTime? Normalize(DateTime? dateTime)
        {
            return dateTime.HasValue ? Normalize(dateTime.Value) : null;
        }

        public static DateTimeOffset Normalize(DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToUniversalTime();
        }

        public static DateTimeOffset? Normalize(DateTimeOffset? dateTimeOffset)
        {
            return dateTimeOffset.HasValue ? Normalize(dateTimeOffset.Value) : null;
        }
    }
}
