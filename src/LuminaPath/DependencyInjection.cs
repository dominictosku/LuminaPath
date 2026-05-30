using LuminaPath.Components;
using LuminaPath.Features.Auth.Account;
using LuminaPath.Features.Documents;
using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace LuminaPath
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBlazor(this IServiceCollection services, IConfiguration config, bool detailedErrors = false)
        {
            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddCircuitOptions(e =>
                {
                    e.DetailedErrors = detailedErrors;
                });

            services.AddCascadingAuthenticationState();
            services.AddScoped<IdentityUserAccessor>();
            services.AddScoped<IdentityRedirectManager>();
            services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
            // When SMTP is configured, AddLuminaIdentity has already registered
            // the real SmtpEmailSender. When it isn't, register the no-op sender
            // that surfaces confirmation links on-screen for development — this
            // overrides the framework's default DefaultMessageEmailSender and is
            // what RegisterConfirmation.razor keys off to show the link.
            if (!EmailOptions.FromConfiguration(config).IsConfigured)
            {
                services.AddSingleton<IEmailSender<LuminaUser>, IdentityNoOpEmailSender>();
            }
            services.AddScoped<DocumentPageService>();

            return services;
        }

        public static void UseBlazor(this WebApplication app)
        {
            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.MapFallback("{*path:nonfile}", RedirectToNotFoundPage)
                .AllowAnonymous();
        }

        private static IResult RedirectToNotFoundPage(HttpContext context)
        {
            if (ShouldPreserveNotFoundStatus(context.Request.Path))
            {
                return Results.NotFound();
            }

            var attemptedPath = (context.Request.PathBase + context.Request.Path).Value ?? "/";
            if (context.Request.QueryString.HasValue)
            {
                attemptedPath += context.Request.QueryString.Value;
            }

            return Results.Redirect($"/NotFound?path={Uri.EscapeDataString(attemptedPath)}");
        }

        private static bool ShouldPreserveNotFoundStatus(PathString path)
        {
            return path.StartsWithSegments("/api")
                || path.StartsWithSegments("/hubs")
                || path.StartsWithSegments("/_blazor")
                || path.StartsWithSegments("/_framework")
                || path.StartsWithSegments("/_content");
        }
    }
}
