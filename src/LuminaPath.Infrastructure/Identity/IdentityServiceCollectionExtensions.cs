using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Identity;

internal static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddLuminaIdentity(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AuthCookieOptions>()
            .Bind(config.GetSection(AuthCookieOptions.SectionName))
            .Validate(AuthCookieOptions.HasValidSameSiteMode, "Auth:CookieSameSite must be a valid SameSiteMode value.")
            .Validate(AuthCookieOptions.HasValidCookieSecurePolicy, "Auth:CookieSecurePolicy must be a valid CookieSecurePolicy value.")
            .ValidateOnStart();

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

        var authCookieOptions = AuthCookieOptions.FromConfiguration(config);
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = authCookieOptions.GetSameSiteMode();
            options.Cookie.SecurePolicy = authCookieOptions.GetCookieSecurePolicy();
        });

        return services;
    }
}
