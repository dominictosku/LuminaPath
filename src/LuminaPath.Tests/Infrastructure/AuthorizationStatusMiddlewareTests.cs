using LuminaPath.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.Tests.Infrastructure;

public class AuthorizationStatusMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RedirectsBrowserUnauthorizedResponsesToLogin()
    {
        var middleware = new AuthorizationStatusMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/Admin/Overview");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/Account/Login?returnUrl=%2FAdmin%2FOverview", context.Response.Headers.Location);
    }

    [Fact]
    public async Task InvokeAsync_RedirectsBrowserForbiddenResponsesToAccessDenied()
    {
        var middleware = new AuthorizationStatusMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/Admin/Users");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/Account/AccessDenied?returnUrl=%2FAdmin%2FUsers", context.Response.Headers.Location);
    }

    [Fact]
    public async Task InvokeAsync_KeepsApiUnauthorizedResponsesAsStatusCodeBodies()
    {
        var middleware = new AuthorizationStatusMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/api/Games");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        Assert.Equal("Session expired, please login", await reader.ReadToEndAsync());
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
