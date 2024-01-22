using Core;
using Core.Interfaces;
using Core.Models;
using Core.Models.Gaming;
using Core.Models.Quests;
using Infrastructure;
using Infrastructure.Interfaces;
using Infrastructure.Repositories;
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
using Server.Services;
using System.Net;
namespace Server
{
    public static class DependencyInjection
	{
		public const string MyAllowSpecificOrigins = "SPAConfig";
		public static IServiceCollection AddServer(this IServiceCollection services, IConfiguration config)
		{
            AddApi(services);
            AddServices(services, config);
			AddCors(services);

            return services;
		}

		public static async Task ConfigureServer(this WebApplication app)
		{
			await ConfigureEnvironment(app);

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

        private static void AddServices(IServiceCollection services, IConfiguration config)
        {
            AddStorageService(services, config);
            AddRepositories(services);
        }

        private static void AddStorageService(IServiceCollection services, IConfiguration config)
		{
            string connectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTIONSTRING")
				?? config.GetSection("Azure")["BlobConnectionString"]
				?? throw new Exception("No blob connectionfound");

            string containerName = config.GetSection("Azure")["BlobContainerName"]
                ?? throw new Exception("No blob container name found");

            services.AddScoped<IAzureStorage, AzureStorage>(s =>
				new AzureStorage(connectionString, containerName, s.GetRequiredService<ILogger<AzureStorage>>()));
        }

		private static void AddRepositories(IServiceCollection services)
		{
            services.AddTransient<IGenericRepo<Game>, GenericRepo<Game>>();
            services.AddTransient<IGenericRepo<MyGame>, GenericRepo<MyGame>>();
            services.AddTransient<IGenericRepo<GamesQuest>, GenericRepo<GamesQuest>>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
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

		private static async Task ConfigureEnvironment(WebApplication app)
		{
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
				await app.MigrateDevelopment();
			}
			else
			{
				app.UseExceptionHandler("/Error", createScopeForErrors: true);
				await app.MigrateDevelopment(); // Temporary add migrations to Production
				app.UseHsts();
			}
		}
	}
}
