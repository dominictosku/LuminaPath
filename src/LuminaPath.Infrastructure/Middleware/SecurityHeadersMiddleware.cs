using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.Infrastructure.Middleware;

/// <summary>
/// Adds a small set of always-on response headers that close off common
/// browser-side attack surface (clickjacking, MIME-sniff downgrades,
/// referrer leaks). Set on every response — including 404s and the
/// SPA bootstrap — so the protections don't disappear on error paths.
///
/// <para>
/// Intentionally NOT setting a Content-Security-Policy here: the Blazor
/// runtime + MudBlazor inject inline styles / eval-style script that
/// require a tuned policy with nonces, and a misconfigured CSP would
/// break the Blazor UI silently. Adding one is a follow-up if/when
/// every script source is auditable.
/// </para>
/// </summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
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

            await next();
        });
    }
}
