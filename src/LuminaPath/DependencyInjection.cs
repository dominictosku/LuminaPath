using LuminaPath.Components;
using LuminaPath.Features.Auth.Account;
using LuminaPath.Features.Documents;
using LuminaPath.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LuminaPath
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBlazor(this IServiceCollection services)
        {
            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddCircuitOptions(e =>
                {
                    e.DetailedErrors = true;
                });

            services.AddCascadingAuthenticationState();
            services.AddScoped<IdentityUserAccessor>();
            services.AddScoped<IdentityRedirectManager>();
            services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
            services.AddSingleton<IEmailSender<LuminaUser>, IdentityNoOpEmailSender>();
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
