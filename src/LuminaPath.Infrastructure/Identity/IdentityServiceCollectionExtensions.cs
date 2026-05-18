using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Identity;

internal static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddLuminaIdentity(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthorization();
        services.AddIdentityApiEndpoints<LuminaUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(120);
            options.Lockout.MaxFailedAccessAttempts = 10;

            options.SignIn.RequireConfirmedAccount = false;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LuminaPathDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = ParseSameSiteMode(config["Auth:CookieSameSite"], SameSiteMode.None);
            options.Cookie.SecurePolicy = ParseCookieSecurePolicy(config["Auth:CookieSecurePolicy"], CookieSecurePolicy.Always);
        });

        return services;
    }

    private static SameSiteMode ParseSameSiteMode(string? value, SameSiteMode fallback)
    {
        return Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private static CookieSecurePolicy ParseCookieSecurePolicy(string? value, CookieSecurePolicy fallback)
    {
        return Enum.TryParse<CookieSecurePolicy>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }
}
