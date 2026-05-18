using System.Net;
using System.Reflection;
using FluentValidation;
using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Hubs;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.Third_Party;
using LuminaPath.Infrastructure.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LuminaPath.Infrastructure
{
    public static class DependencyInjection
    {
        public const string MyAllowSpecificOrigins = "SPAConfig";

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddPersistence(config);
            services.AddLuminaIdentity(config);
            services.AddApplicationServices(config);
            services.AddThirdPartyIntegrations(config);
            services.AddAiChatServices(config);
            services.AddHttpContextAccessor();
            services.AddSingleton<AuditSaveChangesInterceptor>();
            AddCache(services, config);
            AddCors(services, config);
            services.AddSignalR();
            services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, UserIdProvider>();
            services.AddOpenApi();
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }

        private static void AddCors(IServiceCollection services, IConfiguration config)
        {
            services.AddCors(options => options.AddPolicy(MyAllowSpecificOrigins, policy => policy
                .WithOrigins(config.GetCorsOrigins())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()));
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

        public static async Task MigrateDatabase(this WebApplication app)
        {
            using var serviceScope = app.Services.CreateScope();
            var services = serviceScope.ServiceProvider;
            var userManager = services.GetRequiredService<UserManager<LuminaUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var context = services.GetRequiredService<LuminaPathDbContext>();

            try
            {
                context.Database.Migrate();
                await context.SeedDatabase(userManager, roleManager);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error when migrating: ", e);
            }
        }

        private static void AddCache(IServiceCollection services, IConfiguration config)
        {
            var redisConnection = ConfigurationValues.FirstNonEmpty(
                config.GetConnectionString("Redis"),
                config["Redis:ConnectionString"],
                config["REDIS_CONNECTIONSTRING"],
                "localhost:6379");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "LuminaPath:";
            });
        }

        private static async Task ConfigureEnvironment(WebApplication app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            });

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "LuminaPath Api");
                });
                await app.MigrateDatabase();
            }
            else
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                if (app.Configuration.GetValue("Database:RunMigrationsOnStartup", false))
                {
                    await app.MigrateDatabase();
                }

                app.UseHsts();
            }
        }

        public static async Task ConfigureInfrastructure(this WebApplication app)
        {
            AddMiddleware(app);

            app.UseCors(MyAllowSpecificOrigins);
            if (app.Configuration.GetValue("Https:Redirect", app.Environment.IsDevelopment()))
            {
                app.UseHttpsRedirection();
            }

            await ConfigureEnvironment(app);
            app.UseAuthentication();
            app.UseMiddleware<IdentityEndpointAuditMiddleware>();
            app.UseAuthorization();
        }

        public static IServiceCollection AddServer(this IServiceCollection services)
        {
            AddApi(services);
            return services;
        }

        private static void AddApi(IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddControllers(options =>
            {
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
                options.Filters.Add<FluentValidationActionFilter>();
            });
        }
    }
}
