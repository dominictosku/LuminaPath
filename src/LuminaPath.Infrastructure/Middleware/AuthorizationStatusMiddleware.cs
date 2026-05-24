using Microsoft.AspNetCore.Http;

namespace LuminaPath.Infrastructure.Middleware;

public sealed class AuthorizationStatusMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            if (ShouldReturnStatusBody(context.Request))
            {
                await WriteTextResponseAsync(context, "Session expired, please login");
                return;
            }

            RedirectToAccountPage(context, "/Account/Login");
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            if (ShouldReturnStatusBody(context.Request))
            {
                await WriteTextResponseAsync(context, "You have not permission to access this");
                return;
            }

            RedirectToAccountPage(context, "/Account/AccessDenied");
        }
    }

    internal static bool ShouldReturnStatusBody(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api")
            || request.Path.StartsWithSegments("/hubs")
            || request.Path.StartsWithSegments("/_blazor")
            || request.Path.StartsWithSegments("/_framework")
            || request.Path.StartsWithSegments("/_content");
    }

    private static Task WriteTextResponseAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(message);
    }

    private static void RedirectToAccountPage(HttpContext context, string accountPath)
    {
        var returnUrl = BuildReturnUrl(context.Request);
        var redirectUrl = $"{accountPath}?returnUrl={Uri.EscapeDataString(returnUrl)}";
        context.Response.Redirect(redirectUrl);
    }

    private static string BuildReturnUrl(HttpRequest request)
    {
        var returnUrl = (request.PathBase + request.Path).Value;
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            returnUrl = "/";
        }

        if (request.QueryString.HasValue)
        {
            returnUrl += request.QueryString.Value;
        }

        return returnUrl;
    }
}
