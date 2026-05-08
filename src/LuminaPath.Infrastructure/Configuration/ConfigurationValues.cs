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
        return config.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? config.GetSection("FrontendUrls").Get<string[]>()
            ?? SplitList(config["LUMINAPATH_CORS_ORIGINS"])
            ?? SplitList(config["FrontendUrl"])
            ?? ["http://localhost:4200"];
    }
}
