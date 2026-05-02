using System.Globalization;

namespace LuminaPath.Infrastructure.Helper
{
    public static class DateHelper
    {
        public static DateTime GetStartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime ParseISODate(string dateStr)
        {
            try
            {
                // Parse ISO 8601 date string
                DateTime dt = DateTime.Parse(dateStr, null, DateTimeStyles.RoundtripKind);
                return UtcDateTime.Normalize(dt);
            }
            catch (FormatException)
            {
                return DateTime.SpecifyKind(default, DateTimeKind.Utc);
            }
        }
    }
}
