namespace LuminaPath.Infrastructure.Configuration;

internal static class InfrastructureOptionValidation
{
    public static bool IsHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static bool IsValidRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }
}
