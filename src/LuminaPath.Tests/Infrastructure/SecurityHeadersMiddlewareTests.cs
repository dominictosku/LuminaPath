using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.Tests.Infrastructure;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AlwaysSetsBaselineHardeningHeaders()
    {
        var context = await RunAsync(new SecurityHeadersOptions());

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal(
            "camera=(), microphone=(), geolocation=(), payment=()",
            context.Response.Headers["Permissions-Policy"]);
    }

    [Fact]
    public async Task InvokeAsync_DefaultsToReportOnlyContentSecurityPolicy()
    {
        var context = await RunAsync(new SecurityHeadersOptions());

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        var csp = context.Response.Headers["Content-Security-Policy-Report-Only"].ToString();
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("style-src 'self' 'unsafe-inline' https://fonts.googleapis.com", csp);
        Assert.Contains("frame-ancestors 'self'", csp);
    }

    [Fact]
    public async Task InvokeAsync_EmitsEnforcingHeaderWhenReportOnlyDisabled()
    {
        var context = await RunAsync(new SecurityHeadersOptions { ContentSecurityPolicyReportOnly = false });

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"));
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task InvokeAsync_OmitsCspWhenDisabled()
    {
        var context = await RunAsync(new SecurityHeadersOptions { EnableContentSecurityPolicy = false });

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"));
        // Baseline headers stay regardless of the CSP toggle.
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
    }

    [Fact]
    public async Task InvokeAsync_HonorsCustomPolicyAndReportUri()
    {
        var context = await RunAsync(new SecurityHeadersOptions
        {
            ContentSecurityPolicy = "default-src 'none'",
            ReportUri = "/csp-report",
        });

        var csp = context.Response.Headers["Content-Security-Policy-Report-Only"].ToString();
        Assert.Equal("default-src 'none'; report-uri /csp-report", csp);
    }

    private static async Task<DefaultHttpContext> RunAsync(SecurityHeadersOptions options)
    {
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options);

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        return context;
    }
}
