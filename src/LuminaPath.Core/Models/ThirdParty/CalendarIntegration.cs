namespace LuminaPath.Core.Models.ThirdParty
{
    /// <summary>
    /// A user's link to an external calendar provider (currently Google). One
    /// row per user per provider. The refresh token is stored encrypted (via
    /// ASP.NET Data Protection) — never in plaintext. <see cref="CalendarId"/>
    /// is the id of the dedicated "LuminaPath" calendar, filled on first sync.
    /// </summary>
    public class CalendarIntegration
    {
        public int Id { get; set; }

        public string LuminaUserId { get; set; } = string.Empty;

        /// <summary>Provider key, e.g. "google".</summary>
        public string Provider { get; set; } = "google";

        /// <summary>Data-Protection-encrypted OAuth refresh token.</summary>
        public string EncryptedRefreshToken { get; set; } = string.Empty;

        /// <summary>Id of the dedicated calendar we write into (set on first sync).</summary>
        public string? CalendarId { get; set; }

        /// <summary>Email of the linked account, shown in settings.</summary>
        public string? AccountEmail { get; set; }

        public DateTime ConnectedAt { get; set; }

        public DateTime? LastSyncedAt { get; set; }
    }
}
