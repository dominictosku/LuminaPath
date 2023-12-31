using Core;
using Core.Interfaces;
using Core.Models;
using Core.Models.Gaming;
using Core.Models.Quests;
using Infrastructure;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
namespace Server
{
	public static class DependencyInjection
	{
		public const string MyAllowSpecificOrigins = "SPAConfig";
		public static IServiceCollection AddServer(this IServiceCollection services)
		{
			services.AddControllers(options =>
			{
				options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
			});

			services.AddCors(o => o.AddPolicy(MyAllowSpecificOrigins, builder =>
			{
				builder.WithOrigins("http://localhost:3000")
					   .WithOrigins("http://127.0.0.1:3000")
					   .AllowAnyMethod()
					   .AllowAnyHeader()
					   .AllowCredentials();
			}));

			return services;
		}

		public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
		{
			string connectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTIONSTRING")
				?? config.GetSection("Azure")["BlobConnectionString"]
				?? throw new Exception("No blob connectionfound");
			string containerName = config.GetSection("Azure")["BlobContainerName"]
				?? throw new Exception("No blob container name found");

			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();

			services.AddScoped<ITokenGenerator, JwtService>();
			services.AddTransient<IGenericRepo<Game>, GenericRepo<Game>>();
			services.AddTransient<IGenericRepo<MyGame>, GenericRepo<MyGame>>();
			services.AddTransient<IGenericRepo<GamesQuest>, GenericRepo<GamesQuest>>();
			services.AddScoped<IAzureStorage, AzureStorage>(s =>
				new AzureStorage(connectionString, containerName, s.GetRequiredService<ILogger<AzureStorage>>()));
			services.AddTransient<GameRepo>();
			services.AddTransient<MyGameRepo>();

			return services;
		}

		public static async Task ConfigureServer(this WebApplication app)
		{
			app.UseForwardedHeaders(new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
			});

			await ConfigureEnvironment(app);

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

			app.UseCors(MyAllowSpecificOrigins);
			app.UseHttpsRedirection();
			app.MapControllers();
		}

		private static async Task ConfigureEnvironment(WebApplication app)
		{
			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
				await app.MigrateDevelopment();
			}
			else
			{
				await app.MigrateDevelopment(); // Temporary add migrations to Production
				app.UseHsts();
			}
		}
	}
}
