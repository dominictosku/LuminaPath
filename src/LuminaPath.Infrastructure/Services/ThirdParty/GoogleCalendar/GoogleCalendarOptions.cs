using LuminaPath.Infrastructure.Configuration;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// Configures the optional Google Calendar integration. The integration is
/// off unless a Google OAuth client id + secret are supplied (operator must
/// register a Google Cloud OAuth 2.0 Web client and enable the Calendar API).
/// </summary>
public sealed class GoogleCalendarOptions
{
    public const string SectionName = "GoogleCalendar";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary>Optional path to a file holding the client secret (Docker secrets).</summary>
    public string? ClientSecretFile { get; set; }

    /// <summary>Name of the dedicated calendar created for LuminaPath events.</summary>
    public string CalendarName { get; set; } = "LuminaPath";

    /// <summary>
    /// OAuth redirect URI registered with Google. When blank it is derived from
    /// the incoming request as {scheme}://{host}/api/integrations/google/callback
    /// (correct behind the reverse proxy because forwarded headers run first).
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>SPA path the callback redirects back to after linking.</summary>
    public string SettingsReturnPath { get; set; } = "/settings";

    // openid+email let us show "Connected as <email>"; calendar grants sync.
    public string Scope { get; set; } = "openid email https://www.googleapis.com/auth/calendar";

    // Endpoints are overridable so tests can point clients at a stub.
    public string AuthorizationEndpoint { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string RevokeEndpoint { get; set; } = "https://oauth2.googleapis.com/revoke";
    public string CalendarApiBaseUrl { get; set; } = "https://www.googleapis.com/calendar/v3";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    internal static bool HasSecretWhenClientIdSet(GoogleCalendarOptions options)
        => string.IsNullOrWhiteSpace(options.ClientId) || !string.IsNullOrWhiteSpace(options.ClientSecret);

    internal static bool HasValidEndpoints(GoogleCalendarOptions options)
        => InfrastructureOptionValidation.IsHttpUrl(options.AuthorizationEndpoint)
            && InfrastructureOptionValidation.IsHttpUrl(options.TokenEndpoint)
            && InfrastructureOptionValidation.IsHttpUrl(options.RevokeEndpoint)
            && InfrastructureOptionValidation.IsHttpUrl(options.CalendarApiBaseUrl);

    internal static bool HasValidRedirectUriWhenSet(GoogleCalendarOptions options)
        => string.IsNullOrWhiteSpace(options.RedirectUri)
            || InfrastructureOptionValidation.IsHttpUrl(options.RedirectUri);
}
