using Microsoft.AspNetCore.Http;

namespace LuminaPath.Infrastructure.Middleware;

public sealed class ApiClientHeaderMiddleware
{
    public const string HeaderName = "X-Lumina-Client";
    public const string HeaderValue = "web";

    private readonly RequestDelegate _next;

    public ApiClientHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresClientHeader(context.Request) && !HasExpectedClientHeader(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $$"""{"title":"Missing required API client header.","status":400,"detail":"Set the {{HeaderName}} header on unsafe /api requests."}""");
            return;
        }

        await _next(context);
    }

    internal static bool RequiresClientHeader(HttpRequest request)
    {
        return IsApiPath(request.Path) && IsUnsafeMethod(request.Method);
    }

    private static bool HasExpectedClientHeader(HttpRequest request)
    {
        return request.Headers.TryGetValue(HeaderName, out var values)
            && values.Any(value => string.Equals(value, HeaderValue, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUnsafeMethod(string method)
    {
        return !HttpMethods.IsGet(method)
            && !HttpMethods.IsHead(method)
            && !HttpMethods.IsOptions(method);
    }

    private static bool IsApiPath(PathString path)
    {
        var value = path.Value;
        return string.Equals(value, "/api", StringComparison.OrdinalIgnoreCase)
            || value?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true;
    }
}
