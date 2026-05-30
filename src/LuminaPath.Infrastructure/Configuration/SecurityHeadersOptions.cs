using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Configuration;

/// <summary>
/// Configures the response headers emitted by
/// <see cref="Middleware.SecurityHeadersMiddleware"/>.
///
/// <para>
/// The Content-Security-Policy is the only knob with real breakage
/// potential, so it ships in <c>report-only</c> mode by default: browsers
/// log violations to the console (and to <c>ReportUri</c> if set) without
/// blocking anything. Once a deployment's console is clean, set
/// <c>SecurityHeaders:ContentSecurityPolicyReportOnly=false</c> to enforce.
/// </para>
/// </summary>
public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>Emit a CSP header at all. Defaults to on (report-only).</summary>
    public bool EnableContentSecurityPolicy { get; set; } = true;

    /// <summary>
    /// When true, send <c>Content-Security-Policy-Report-Only</c> instead of
    /// the enforcing <c>Content-Security-Policy</c>. Defaults to true so the
    /// UI can never break silently before the policy has been validated.
    /// </summary>
    public bool ContentSecurityPolicyReportOnly { get; set; } = true;

    /// <summary>
    /// Override the policy string. When blank, <see cref="DefaultPolicy"/> is
    /// used (tuned for the Blazor Server host).
    /// </summary>
    public string? ContentSecurityPolicy { get; set; }

    /// <summary>Optional reporting endpoint appended as a <c>report-uri</c> directive.</summary>
    public string? ReportUri { get; set; }

    /// <summary>
    /// Default policy tuned for the App.razor host: every script is
    /// same-origin, MudBlazor/Radzen inject inline styles at runtime
    /// (hence <c>'unsafe-inline'</c> for styles only), and Google Fonts is
    /// the single external origin. Blazor Server's SignalR transport is
    /// same-origin, so <c>connect-src 'self'</c> covers the WebSocket.
    /// </summary>
    public const string DefaultPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "script-src 'self'; " +
        "connect-src 'self'; " +
        "form-action 'self'";

    public string GetEffectivePolicy()
    {
        var policy = string.IsNullOrWhiteSpace(ContentSecurityPolicy)
            ? DefaultPolicy
            : ContentSecurityPolicy.Trim();

        return string.IsNullOrWhiteSpace(ReportUri)
            ? policy
            : $"{policy}; report-uri {ReportUri.Trim()}";
    }

    public string CspHeaderName => ContentSecurityPolicyReportOnly
        ? "Content-Security-Policy-Report-Only"
        : "Content-Security-Policy";

    public static SecurityHeadersOptions FromConfiguration(IConfiguration config)
        => config.GetSection(SectionName).Get<SecurityHeadersOptions>() ?? new SecurityHeadersOptions();
}
