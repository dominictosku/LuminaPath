using Domain.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Server
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddServer(this IServiceCollection services)
		{
			AddApi(services);
			return services;
		}

		private static void AddApi(IServiceCollection services)
		{
			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();
			services.AddControllers(options =>
			{
				options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
			});
		}

		public static void ConfigureServer(this WebApplication app)
		{
			app.MapControllers();
			app.MapGroup("/api")
				.MapIdentityApi<LuminaUser>();
		}
	}
}
