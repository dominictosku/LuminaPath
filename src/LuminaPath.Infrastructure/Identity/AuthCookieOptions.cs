using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Identity;

public sealed class AuthCookieOptions
{
    public const string SectionName = "Auth";

    public string CookieSameSite { get; set; } = SameSiteMode.None.ToString();
    public string CookieSecurePolicy { get; set; } = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always.ToString();

    internal static AuthCookieOptions FromConfiguration(IConfiguration config)
    {
        var options = new AuthCookieOptions();
        config.GetSection(SectionName).Bind(options);
        return options;
    }

    internal SameSiteMode GetSameSiteMode()
    {
        return Enum.Parse<SameSiteMode>(CookieSameSite, ignoreCase: true);
    }

    internal CookieSecurePolicy GetCookieSecurePolicy()
    {
        return Enum.Parse<CookieSecurePolicy>(CookieSecurePolicy, ignoreCase: true);
    }

    internal static bool HasValidSameSiteMode(AuthCookieOptions options)
    {
        return Enum.TryParse<SameSiteMode>(options.CookieSameSite, ignoreCase: true, out _);
    }

    internal static bool HasValidCookieSecurePolicy(AuthCookieOptions options)
    {
        return Enum.TryParse<CookieSecurePolicy>(options.CookieSecurePolicy, ignoreCase: true, out _);
    }
}
