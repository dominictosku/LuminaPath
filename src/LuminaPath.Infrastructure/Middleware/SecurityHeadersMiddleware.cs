using LuminaPath.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.Infrastructure.Middleware;

/// <summary>
/// Adds a small set of always-on response headers that close off common
/// browser-side attack surface (clickjacking, MIME-sniff downgrades,
/// referrer leaks) plus a configurable Content-Security-Policy. Set on
/// every response — including 404s and the SPA bootstrap — so the
/// protections don't disappear on error paths.
///
/// <para>
/// The CSP defaults to <c>report-only</c> (see <see cref="SecurityHeadersOptions"/>):
/// the Blazor runtime + MudBlazor/Radzen inject inline styles, so the
/// default policy allows <c>style-src 'unsafe-inline'</c> while keeping
/// scripts locked to <c>'self'</c>. Report-only means a misconfigured
/// policy surfaces as console warnings instead of a broken UI; flip
/// <c>ContentSecurityPolicyReportOnly</c> to false to enforce.
/// </para>
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _cspHeaderName;
    private readonly string? _cspValue;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
    {
        _next = next;

        if (options.EnableContentSecurityPolicy)
        {
            _cspHeaderName = options.CspHeaderName;
            _cspValue = options.GetEffectivePolicy();
        }
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Block browsers from second-guessing the declared content
        // type (the classic "your image is actually HTML" attack).
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent the app from being framed by other origins —
        // defangs clickjacking against the Blazor admin shell.
        // SAMEORIGIN (not DENY) so internal Blazor flows can still
        // embed their own pages if they ever need to.
        headers["X-Frame-Options"] = "SAMEORIGIN";

        // Trim what we leak in the Referer header for cross-origin
        // navigations — avoid sending full URLs (which may contain
        // resource IDs) to third-party sites.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Block opt-in to legacy browser sensors / geo / camera /
        // mic / payment APIs we never use. Cheap defense-in-depth
        // against future XSS.
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        if (_cspHeaderName is not null && _cspValue is not null)
        {
            headers[_cspHeaderName] = _cspValue;
        }

        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, SecurityHeadersOptions options)
        => app.UseMiddleware<SecurityHeadersMiddleware>(options);
}
