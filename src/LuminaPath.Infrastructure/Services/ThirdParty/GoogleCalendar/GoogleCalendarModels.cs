namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>Tokens returned by the Google OAuth token endpoint.</summary>
public sealed record GoogleTokenResult(string AccessToken, string? RefreshToken, string? Email);

/// <summary>A LuminaPath-managed calendar event to upsert.</summary>
public sealed record CalendarEventInput(
    string Id,
    string Summary,
    string? Description,
    DateOnly Date,
    DateTime? StartAt = null,
    DateTime? EndAt = null)
{
    public bool IsTimed => StartAt.HasValue && EndAt.HasValue;

    public static CalendarEventInput AllDay(string id, string summary, string? description, DateOnly date)
        => new(id, summary, description, date);

    public static CalendarEventInput Timed(string id, string summary, string? description, DateTime startAt, DateTime endAt)
        => new(id, summary, description, DateOnly.FromDateTime(startAt), startAt, endAt);
}

/// <summary>Outcome of a sync run, surfaced to the caller for a status message.</summary>
public sealed record CalendarSyncResult(int ReleaseEvents, int QuestEvents, int SessionEvents, int Deleted);

/// <summary>Current link state for the settings UI.</summary>
public sealed record CalendarConnectionStatus(bool Connected, string? Email, DateTime? LastSyncedAt);
