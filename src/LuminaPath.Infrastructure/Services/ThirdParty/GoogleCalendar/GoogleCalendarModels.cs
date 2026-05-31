namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>Tokens returned by the Google OAuth token endpoint.</summary>
public sealed record GoogleTokenResult(string AccessToken, string? RefreshToken, string? Email);

/// <summary>A LuminaPath-managed all-day calendar event to upsert.</summary>
public sealed record CalendarEventInput(string Id, string Summary, string? Description, DateOnly Date);

/// <summary>Outcome of a sync run, surfaced to the caller for a status message.</summary>
public sealed record CalendarSyncResult(int ReleaseEvents, int QuestEvents, int Deleted);

/// <summary>Current link state for the settings UI.</summary>
public sealed record CalendarConnectionStatus(bool Connected, string? Email, DateTime? LastSyncedAt);
