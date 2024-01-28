using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
namespace Application
{
	public static class DependencyInjection
	{
		public const string MyAllowSpecificOrigins = "SPAConfig";
		public static IServiceCollection AddServer(this IServiceCollection services)
		{
			AddApi(services);
			AddCors(services);

			return services;
		}

		public static void ConfigureServer(this WebApplication app)
		{
			AddMiddleware(app);

			app.UseCors(MyAllowSpecificOrigins);
			app.UseHttpsRedirection();
			app.MapControllers();
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

		private static void AddCors(IServiceCollection services)
		{
			services.AddCors(o => o.AddPolicy(MyAllowSpecificOrigins, builder =>
			{
				builder.WithOrigins("http://localhost:3000")
					   .WithOrigins("http://127.0.0.1:3000")
					   .AllowAnyMethod()
					   .AllowAnyHeader()
					   .AllowCredentials();
			}));
		}

		private static void AddMiddleware(WebApplication app)
		{
			app.Use(async (context, next) =>
			{
				await next();

				if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
				{
					await context.Response.WriteAsync("Session expired, please login");
				}

				if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
				{
					await context.Response.WriteAsync("You have not permission to access this");
				}
			});
		}
	}
}
