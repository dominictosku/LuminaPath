using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// Resolves the effective Google Calendar settings, preferring values an admin
/// saved in <c>ApplicationSettings</c> (editable from the admin Settings page)
/// over the env/config defaults in <see cref="GoogleCalendarOptions"/>. Mirrors
/// the DB-over-IOptions pattern used for IGDB credentials and background jobs,
/// so credentials can be configured without an environment variable or restart.
/// </summary>
public sealed class GoogleCalendarSettingsResolver
{
    private readonly GoogleCalendarOptions _options;
    private readonly ApplicationSettingsService _settings;

    public GoogleCalendarSettingsResolver(IOptions<GoogleCalendarOptions> options, ApplicationSettingsService settings)
    {
        _options = options.Value;
        _settings = settings;
    }

    public async Task<GoogleCalendarEffectiveSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings.GetGoogleCalendarSettingsAsync(cancellationToken);
        return new GoogleCalendarEffectiveSettings(
            ClientId: Coalesce(stored.ClientId, _options.ClientId),
            ClientSecret: Coalesce(stored.ClientSecret, _options.ClientSecret),
            CalendarName: string.IsNullOrWhiteSpace(stored.CalendarName) ? _options.CalendarName : stored.CalendarName,
            Scope: _options.Scope,
            AuthorizationEndpoint: _options.AuthorizationEndpoint,
            TokenEndpoint: _options.TokenEndpoint,
            RevokeEndpoint: _options.RevokeEndpoint);
    }

    // Prefer the stored value; fall back to the env/config value when blank.
    private static string? Coalesce(string stored, string? fallback)
        => string.IsNullOrWhiteSpace(stored) ? fallback : stored;
}

/// <summary>
/// Effective Google Calendar settings (DB overrides merged over env/config).
/// </summary>
public sealed record GoogleCalendarEffectiveSettings(
    string? ClientId,
    string? ClientSecret,
    string CalendarName,
    string Scope,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string RevokeEndpoint)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
