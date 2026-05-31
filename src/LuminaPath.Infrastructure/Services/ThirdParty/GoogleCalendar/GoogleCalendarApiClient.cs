using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// Thin Google Calendar v3 client: find/create the dedicated calendar, list
/// its events, and upsert/delete events. Events use deterministic ids so a
/// re-sync updates in place instead of duplicating.
/// </summary>
public interface IGoogleCalendarApi
{
    Task<string> EnsureCalendarAsync(string accessToken, string calendarName, string? knownCalendarId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListEventIdsAsync(string accessToken, string calendarId, CancellationToken cancellationToken);
    Task UpsertEventAsync(string accessToken, string calendarId, CalendarEventInput calendarEvent, CancellationToken cancellationToken);
    Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken cancellationToken);
}

public sealed class GoogleCalendarApiClient : IGoogleCalendarApi
{
    private readonly HttpClient _http;
    private readonly GoogleCalendarOptions _options;

    public GoogleCalendarApiClient(HttpClient http, IOptions<GoogleCalendarOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> EnsureCalendarAsync(string accessToken, string calendarName, string? knownCalendarId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(knownCalendarId))
        {
            using var existing = await SendAsync(HttpMethod.Get, accessToken, $"calendars/{Uri.EscapeDataString(knownCalendarId)}", null, cancellationToken);
            if (existing.IsSuccessStatusCode)
            {
                return knownCalendarId;
            }
        }

        // Reuse a calendar with our name if the user already has one.
        var found = await FindCalendarByNameAsync(accessToken, calendarName, cancellationToken);
        if (found is not null)
        {
            return found;
        }

        using var created = await SendAsync(HttpMethod.Post, accessToken, "calendars", new { summary = calendarName }, cancellationToken);
        created.EnsureSuccessStatusCode();
        using var doc = await ReadJsonAsync(created, cancellationToken);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Google did not return a calendar id.");
    }

    public async Task<IReadOnlyList<string>> ListEventIdsAsync(string accessToken, string calendarId, CancellationToken cancellationToken)
    {
        var ids = new List<string>();
        string? pageToken = null;

        do
        {
            var query = $"calendars/{Uri.EscapeDataString(calendarId)}/events?maxResults=2500&showDeleted=false";
            if (pageToken is not null)
            {
                query += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            }

            using var response = await SendAsync(HttpMethod.Get, accessToken, query, null, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = await ReadJsonAsync(response, cancellationToken);
            var root = doc.RootElement;

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    {
                        ids.Add(id.GetString()!);
                    }
                }
            }

            pageToken = root.TryGetProperty("nextPageToken", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
        }
        while (pageToken is not null);

        return ids;
    }

    public async Task UpsertEventAsync(string accessToken, string calendarId, CalendarEventInput calendarEvent, CancellationToken cancellationToken)
    {
        var body = BuildEventBody(calendarEvent, includeId: false);
        var path = $"calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(calendarEvent.Id)}";

        // Update in place when the event already exists; create it on first sync.
        using var patch = await SendAsync(HttpMethod.Patch, accessToken, path, body, cancellationToken);
        if (patch.IsSuccessStatusCode)
        {
            return;
        }

        if (patch.StatusCode != HttpStatusCode.NotFound)
        {
            patch.EnsureSuccessStatusCode();
        }

        var insertBody = BuildEventBody(calendarEvent, includeId: true);
        using var insert = await SendAsync(HttpMethod.Post, accessToken, $"calendars/{Uri.EscapeDataString(calendarId)}/events", insertBody, cancellationToken);
        insert.EnsureSuccessStatusCode();
    }

    public async Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            accessToken,
            $"calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(eventId)}",
            null,
            cancellationToken);

        // 404/410 mean it's already gone — fine for a reconcile.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private async Task<string?> FindCalendarByNameAsync(string accessToken, string calendarName, CancellationToken cancellationToken)
    {
        string? pageToken = null;
        do
        {
            var query = "users/me/calendarList?maxResults=250";
            if (pageToken is not null)
            {
                query += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            }

            using var response = await SendAsync(HttpMethod.Get, accessToken, query, null, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = await ReadJsonAsync(response, cancellationToken);
            var root = doc.RootElement;

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var summary = item.TryGetProperty("summary", out var s) ? s.GetString() : null;
                    if (string.Equals(summary, calendarName, StringComparison.Ordinal)
                        && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    {
                        return id.GetString();
                    }
                }
            }

            pageToken = root.TryGetProperty("nextPageToken", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
        }
        while (pageToken is not null);

        return null;
    }

    private static object BuildEventBody(CalendarEventInput calendarEvent, bool includeId)
    {
        // All-day event: end.date is exclusive, so it's the day after start.
        var start = calendarEvent.Date.ToString("yyyy-MM-dd");
        var end = calendarEvent.Date.AddDays(1).ToString("yyyy-MM-dd");

        if (includeId)
        {
            return new
            {
                id = calendarEvent.Id,
                summary = calendarEvent.Summary,
                description = calendarEvent.Description,
                start = new { date = start },
                end = new { date = end },
            };
        }

        return new
        {
            summary = calendarEvent.Summary,
            description = calendarEvent.Description,
            start = new { date = start },
            end = new { date = end },
        };
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string accessToken, string relativePath, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, $"{_options.CalendarApiBaseUrl}/{relativePath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return _http.SendAsync(request, cancellationToken);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
