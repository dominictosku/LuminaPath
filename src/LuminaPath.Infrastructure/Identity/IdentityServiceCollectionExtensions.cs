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
            .Validate(
                AuthCookieOptions.HasSecureCrossSiteCookiePolicy,
                "Auth:CookieSecurePolicy must be Always when Auth:CookieSameSite is None.")
            .ValidateOnStart();

        // Self-registration knobs. Reads Auth:RequireAdminApproval from
        // config / env (Auth__RequireAdminApproval=true). Consumed by
        // LuminaUserManager when a new user comes in via an anonymous
        // request.
        services.AddOptions<AuthRegistrationOptions>()
            .Bind(config.GetSection(AuthRegistrationOptions.SectionName));

        services.AddAuthorization(options =>
        {
            // Used by the catalog controllers' mutation endpoints (Games,
            // Animes, Series, Movies) — POST/PUT/DELETE require an
            // Administrator or Editor role. Mirrors the Blazor app's
            // BaseAuth.CanUserEdit check.
            options.AddPolicy(AuthorizationPolicies.CatalogEditors, policy =>
                policy.RequireRole("Administrator", "Editor"));
        });
        services.AddIdentityApiEndpoints<LuminaUser>(options =>
        {
            // Hardened production password policy. RequireConfirmedAccount
            // stays off because we ship without an SMTP sender; the
            // RequireAdminApproval gate is the human-in-the-loop check.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 10;
            options.Password.RequiredUniqueChars = 4;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            // Critical for the lockout policy to take effect on NEW users —
            // without this every freshly-created user has LockoutEnabled=false
            // and would never be auto-locked on failed attempts.
            options.Lockout.AllowedForNewUsers = true;

            options.SignIn.RequireConfirmedAccount = false;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LuminaPathDbContext>()
            .AddUserManager<LuminaUserManager>()
            .AddSignInManager<LuminaSignInManager>()
            .AddDefaultTokenProviders();

        var authCookieOptions = AuthCookieOptions.FromConfiguration(config);
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.SameSite = authCookieOptions.GetSameSiteMode();
            options.Cookie.SecurePolicy = authCookieOptions.GetCookieSecurePolicy();
            options.Events.OnRedirectToLogin = context =>
            {
                if (ShouldReturnStatusCode(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (ShouldReturnStatusCode(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        return services;
    }

    private static bool ShouldReturnStatusCode(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api")
            || request.Path.StartsWithSegments("/hubs")
            || request.Path.StartsWithSegments("/_blazor")
            || request.Path.StartsWithSegments("/_framework")
            || request.Path.StartsWithSegments("/_content");
    }
}
