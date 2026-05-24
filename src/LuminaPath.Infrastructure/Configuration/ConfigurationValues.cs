using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Configuration;

internal static class ConfigurationValues
{
    public static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    public static string[]? SplitList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static void UseEnvironmentFallback(this string? currentValue, Action<string> apply, string environmentVariable)
    {
        if (!string.IsNullOrWhiteSpace(currentValue))
        {
            return;
        }

        var fallback = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            apply(fallback);
        }
    }

    public static string[] GetCorsOrigins(this IConfiguration config)
    {
        var configured = NormalizeOrigins(config.GetSection("Cors:AllowedOrigins").Get<string[]>())
            ?? NormalizeOrigins(config.GetSection("FrontendUrls").Get<string[]>())
            ?? NormalizeOrigins(SplitList(config["LUMINAPATH_CORS_ORIGINS"]))
            ?? NormalizeOrigins(SplitList(config["FrontendUrl"]));

        if (configured is not null)
        {
            return configured;
        }

        var environment = config["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            ? ["http://localhost:4200"]
            : [];
    }

    private static string[]? NormalizeOrigins(string[]? origins)
    {
        var normalized = origins?
            .Select(origin => origin.Trim())
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized is { Length: > 0 } ? normalized : null;
    }
}
