using LuminaPath.Core.Models.ThirdParty;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// One-way push of the user's library releases, open quests, and open gaming
/// sessions into their dedicated "LuminaPath" Google Calendar. Idempotent:
/// events use deterministic ids so a re-sync updates in place, and events no
/// longer backed by LuminaPath data are deleted. The calendar is exclusively
/// managed by LuminaPath.
/// </summary>
public sealed class GoogleCalendarSyncService
{
    public const string Provider = "google";
    private const string ReleaseIdPrefix = "lprel";
    private const string QuestIdPrefix = "lpquest";
    private const string SessionIdPrefix = "lpsess";

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IGoogleOAuthClient _oauth;
    private readonly IGoogleCalendarApi _calendar;
    private readonly ICalendarTokenProtector _protector;
    private readonly GoogleCalendarSettingsResolver _resolver;
    private readonly Func<DateTime> _utcNow;

    public GoogleCalendarSyncService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IGoogleOAuthClient oauth,
        IGoogleCalendarApi calendar,
        ICalendarTokenProtector protector,
        GoogleCalendarSettingsResolver resolver)
        : this(dbContextFactory, oauth, calendar, protector, resolver, () => DateTime.UtcNow)
    {
    }

    public GoogleCalendarSyncService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IGoogleOAuthClient oauth,
        IGoogleCalendarApi calendar,
        ICalendarTokenProtector protector,
        GoogleCalendarSettingsResolver resolver,
        Func<DateTime> utcNow)
    {
        _dbContextFactory = dbContextFactory;
        _oauth = oauth;
        _calendar = calendar;
        _protector = protector;
        _resolver = resolver;
        _utcNow = utcNow;
    }

    public async Task<CalendarSyncResult> SyncAsync(string userId, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var link = await db.CalendarIntegrations
            .FirstOrDefaultAsync(c => c.LuminaUserId == userId && c.Provider == Provider, cancellationToken)
            ?? throw new InvalidOperationException("Google Calendar is not connected for this user.");

        var settings = await _resolver.GetAsync(cancellationToken);
        var refreshToken = _protector.Unprotect(link.EncryptedRefreshToken);
        var token = await _oauth.RefreshAccessTokenAsync(refreshToken, cancellationToken);

        var calendarId = await _calendar.EnsureCalendarAsync(token.AccessToken, settings.CalendarName, link.CalendarId, cancellationToken);
        if (!string.Equals(calendarId, link.CalendarId, StringComparison.Ordinal))
        {
            link.CalendarId = calendarId;
        }

        var releases = await BuildReleaseEventsAsync(db, userId, cancellationToken);
        var quests = await BuildQuestEventsAsync(db, userId, cancellationToken);
        var sessions = await BuildSessionEventsAsync(db, userId, cancellationToken);
        var desired = releases
            .Concat(quests)
            .Concat(sessions)
            .ToList();

        var existingIds = await _calendar.ListEventIdsAsync(token.AccessToken, calendarId, cancellationToken);

        foreach (var calendarEvent in desired)
        {
            await _calendar.UpsertEventAsync(token.AccessToken, calendarId, calendarEvent, cancellationToken);
        }

        var desiredIds = desired.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var stale = existingIds.Where(id => !desiredIds.Contains(id)).ToList();
        foreach (var staleId in stale)
        {
            await _calendar.DeleteEventAsync(token.AccessToken, calendarId, staleId, cancellationToken);
        }

        link.LastSyncedAt = _utcNow();
        await db.SaveChangesAsync(cancellationToken);

        return new CalendarSyncResult(releases.Count, quests.Count, sessions.Count, stale.Count);
    }

    private async Task<List<CalendarEventInput>> BuildReleaseEventsAsync(
        LuminaPathDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_utcNow().Date);

        var rows = await db.MyGames
            .AsNoTracking()
            .Where(m => m.LuminaUserId == userId && m.Game!.ReleaseDate.HasValue && m.Game.ReleaseDate.Value >= _utcNow().Date)
            .Select(m => new { m.Game!.Id, m.Game.Name, m.Game.ReleaseDate })
            .ToListAsync(cancellationToken);

        return rows
            // One game can appear once; collapse any duplicate library rows.
            .GroupBy(r => r.Id)
            .Select(group => group.First())
            .Select(r => CalendarEventInput.AllDay(
                id: ReleaseIdPrefix + r.Id,
                summary: $"Game release: {r.Name}",
                description: "Game release tracked in your LuminaPath library.",
                date: DateOnly.FromDateTime(r.ReleaseDate!.Value)))
            .Where(e => e.Date >= today)
            .ToList();
    }

    private async Task<List<CalendarEventInput>> BuildQuestEventsAsync(
        LuminaPathDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        var rows = await db.Quests
            .AsNoTracking()
            .Where(q => q.LuminaUserId == userId
                && !q.Completed
                && (q.DueDate.HasValue || (q.ScheduledStartAt.HasValue && q.ScheduledEndAt.HasValue)))
            .Select(q => new { q.Id, q.Title, q.DueDate, q.ScheduledStartAt, q.ScheduledEndAt })
            .ToListAsync(cancellationToken);

        return rows
            .Select(q =>
            {
                if (q.ScheduledStartAt.HasValue && q.ScheduledEndAt.HasValue)
                {
                    return CalendarEventInput.Timed(
                        id: QuestIdPrefix + q.Id,
                        summary: $"Quest: {q.Title}",
                        description: "Scheduled quest from your LuminaPath weekly schedule.",
                        startAt: q.ScheduledStartAt.Value,
                        endAt: q.ScheduledEndAt.Value);
                }

                return CalendarEventInput.AllDay(
                    id: QuestIdPrefix + q.Id,
                    summary: $"Quest: {q.Title}",
                    description: "Quest due date from your LuminaPath quest board.",
                    date: DateOnly.FromDateTime(q.DueDate!.Value));
            })
            .ToList();
    }

    private async Task<List<CalendarEventInput>> BuildSessionEventsAsync(
        LuminaPathDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        var rows = await db.GamingSessions
            .AsNoTracking()
            .Include(session => session.MyGame!)
                .ThenInclude(myGame => myGame.Game)
            .Where(session => session.LuminaUserId == userId
                && !session.Completed
                && session.DurationMinutes > 0)
            .ToListAsync(cancellationToken);

        return rows
            .Select(session =>
            {
                var title = session.MyGame?.Game?.Name ?? "Gaming session";
                var start = session.ScheduledAt;
                var end = start.AddMinutes(session.DurationMinutes);
                var description = string.IsNullOrWhiteSpace(session.Notes)
                    ? "Gaming session scheduled in LuminaPath."
                    : session.Notes.Trim();

                return CalendarEventInput.Timed(
                    id: SessionIdPrefix + session.Id,
                    summary: $"Gaming session: {title}",
                    description: description,
                    startAt: start,
                    endAt: end);
            })
            .ToList();
    }
}
