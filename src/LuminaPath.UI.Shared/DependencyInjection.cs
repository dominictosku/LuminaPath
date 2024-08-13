using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace LuminaPath.UI.Shared
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddUILibrary(this IServiceCollection services)
		{
			services.AddMudServices();
			return services;
		}
	}
}
