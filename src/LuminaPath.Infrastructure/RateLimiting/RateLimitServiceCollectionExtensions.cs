using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.RateLimiting;

internal static class RateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddLuminaPathRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton<UserActionRateLimiter>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers["Retry-After"] =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please wait and try again.",
                    cancellationToken);
            };

            AddAuthenticatedPolicy(options, RateLimitPolicies.ChatStreaming, permitLimit: 10, TimeSpan.FromMinutes(1));
            AddAuthenticatedPolicy(options, RateLimitPolicies.Imports, permitLimit: 6, TimeSpan.FromHours(1));
            AddAuthenticatedPolicy(options, RateLimitPolicies.Uploads, permitLimit: 30, TimeSpan.FromMinutes(10));
            AddAuthenticatedPolicy(options, RateLimitPolicies.DirectMessages, permitLimit: 60, TimeSpan.FromMinutes(1));
            AddAuthenticatedPolicy(options, RateLimitPolicies.BroadReads, permitLimit: 120, TimeSpan.FromMinutes(1));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var profile = GetAuthRateLimitProfile(context.Request);
                if (profile is null)
                {
                    return RateLimitPartition.GetNoLimiter("non-auth");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    $"{profile.Name}:{GetClientIp(context)}",
                    _ => CreateFixedWindowOptions(profile.PermitLimit, profile.Window));
            });
        });

        return services;
    }

    private static void AddAuthenticatedPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window)
    {
        options.AddPolicy(policyName, context => RateLimitPartition.GetFixedWindowLimiter(
            GetUserOrIpPartitionKey(context, policyName),
            _ => CreateFixedWindowOptions(permitLimit, window)));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(int permitLimit, TimeSpan window)
    {
        return new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            Window = window
        };
    }

    private static string GetUserOrIpPartitionKey(HttpContext context, string policyName)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorKey = string.IsNullOrWhiteSpace(userId)
            ? $"ip:{GetClientIp(context)}"
            : $"user:{userId}";

        return $"{policyName}:{actorKey}";
    }

    private static AuthRateLimitProfile? GetAuthRateLimitProfile(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return null;
        }

        var path = request.Path.Value ?? string.Empty;
        if (IsPath(path, "/api/login")
            || IsPath(path, "/Account/Login")
            || IsPath(path, "/Account/LoginWith2fa")
            || IsPath(path, "/Account/LoginWithRecoveryCode"))
        {
            return new AuthRateLimitProfile("login", 10, TimeSpan.FromMinutes(5));
        }

        if (IsPath(path, "/api/register")
            || IsPath(path, "/Account/Register")
            || IsPath(path, "/Account/ExternalLogin"))
        {
            return new AuthRateLimitProfile("register", 5, TimeSpan.FromHours(1));
        }

        if (IsPath(path, "/api/forgotPassword")
            || IsPath(path, "/api/resendConfirmationEmail")
            || IsPath(path, "/Account/ForgotPassword"))
        {
            return new AuthRateLimitProfile("email", 5, TimeSpan.FromHours(1));
        }

        return null;
    }

    private static bool IsPath(string actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private sealed record AuthRateLimitProfile(string Name, int PermitLimit, TimeSpan Window);
}
