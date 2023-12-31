using Core.Models;
using LuminaPath.Components;
using LuminaPath.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LuminaPath
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBlazor(this IServiceCollection services)
        {
            services.AddRazorComponents()
                .AddInteractiveServerComponents();

            services.AddCascadingAuthenticationState();
            services.AddScoped<IdentityUserAccessor>();
            services.AddScoped<IdentityRedirectManager>();
            services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
            services.AddSingleton<IEmailSender<LuminaUser>, IdentityNoOpEmailSender>();

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
        }
    }
}
