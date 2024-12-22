using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Radzen;

namespace LuminaPath.UI.Shared
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddUILibrary(this IServiceCollection services)
		{
			services.AddMudServices();
            services.AddRadzenComponents();
            return services;
		}
	}
}
