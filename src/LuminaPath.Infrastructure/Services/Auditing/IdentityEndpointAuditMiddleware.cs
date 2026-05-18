using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.Infrastructure.Services.Auditing;

public sealed class IdentityEndpointAuditMiddleware
{
    private readonly RequestDelegate _next;

    public IdentityEndpointAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuditLogService auditLogService)
    {
        var request = GetTrackedRequest(context);
        if (request is null)
        {
            await _next(context);
            return;
        }

        var email = await TryReadEmailAsync(context);
        var actor = request.Action == AuditActions.Logout
            ? AuditLogService.ActorFromPrincipal(context.User)
            : null;

        try
        {
            await _next(context);

            await auditLogService.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Account,
                Action = request.Action,
                Outcome = context.Response.StatusCode < 400 ? AuditOutcomes.Success : AuditOutcomes.Failure,
                Actor = actor,
                TargetType = "User",
                TargetName = email,
                Metadata = new
                {
                    source = "IdentityApi",
                    statusCode = context.Response.StatusCode
                },
                ErrorMessage = context.Response.StatusCode < 400 ? null : $"HTTP {context.Response.StatusCode}"
            });
        }
        catch (Exception ex)
        {
            await auditLogService.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Account,
                Action = request.Action,
                Outcome = AuditOutcomes.Failure,
                Actor = actor,
                TargetType = "User",
                TargetName = email,
                Metadata = new { source = "IdentityApi" },
                ErrorMessage = ex.Message
            });
            throw;
        }
    }

    private static TrackedIdentityRequest? GetTrackedRequest(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return null;
        }

        var path = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant();
        return path switch
        {
            "/api/login" => new TrackedIdentityRequest(AuditActions.Login),
            "/api/register" => new TrackedIdentityRequest(AuditActions.Registration),
            "/api/logout" => new TrackedIdentityRequest(AuditActions.Logout),
            _ => null
        };
    }

    private static async Task<string?> TryReadEmailAsync(HttpContext context)
    {
        if (!context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
        }

        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String
                ? email.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record TrackedIdentityRequest(string Action);
}
