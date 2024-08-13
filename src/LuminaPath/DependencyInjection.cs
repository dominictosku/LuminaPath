using LuminaPath.Components;
using LuminaPath.Core.Models;
using LuminaPath.Pages.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;

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
