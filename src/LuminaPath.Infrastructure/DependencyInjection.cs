using System.Reflection;
using FluentValidation;
using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Hubs;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Middleware;
using LuminaPath.Infrastructure.RateLimiting;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ThirdParty;
using LuminaPath.Infrastructure.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure
{
    public static class DependencyInjection
    {
        public const string MyAllowSpecificOrigins = "SPAConfig";

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpContextAccessor();
            services.AddAuditingServices();
            services.AddPersistence(config);
            services.AddLuminaIdentity(config);
            services.AddApplicationServices(config);
            services.AddThirdPartyIntegrations(config);
            services.AddAiChatServices(config);
            AddCache(services, config);
            AddCors(services, config);
            services.AddHostedService<DeploymentConfigurationWarningService>();
            services.AddLuminaPathRateLimiting();
            services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 16 * 1024;
            });
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
                .AllowCredentials()
                // Allowlist the custom header that LuminaSignInManager
                // sets on a 401 from /api/login when the user's account
                // is inactive — without this, the browser hides it from
                // the Angular SPA on cross-origin requests and the
                // tailored "awaiting approval" message can't be shown.
                // Export filenames travel in download response headers and
                // need the same treatment when the Angular app is cross-origin.
                .WithExposedHeaders(
                    LoginBlockedReason.ResponseHeaderName,
                    "Content-Disposition",
                    DataExportController.ExportFileNameHeader)));
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
                // Hand seed credentials through config so operators can
                // override admin@example.com / weak default password
                // before first boot. The seeder logs a warning when the
                // bundled fallback is in use so it's obvious to change.
                await context.SeedDatabase(
                    userManager,
                    roleManager,
                    app.Configuration,
                    app.Logger,
                    requireConfiguredAdminCredentials: !app.Environment.IsDevelopment());
            }
            catch (Exception e)
            {
                // Fail loudly: a half-applied or un-applied schema must stop
                // startup rather than let the app serve a broken database.
                // The top-level handler in Program.cs logs Fatal and exits.
                app.Logger.LogCritical(e, "Database migration or seeding failed during startup.");
                throw;
            }
        }

        private static void AddCache(IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<RedisOptions>()
                .Bind(config.GetSection(RedisOptions.SectionName))
                .PostConfigure(options => RedisOptions.ApplyFallbacks(options, config))
                .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Redis connection string is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.InstanceName), "Redis instance name is required.")
                .ValidateOnStart();

            var redisOptions = RedisOptions.FromConfiguration(config);

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstanceName;
            });
        }

        private static async Task ConfigureEnvironment(WebApplication app)
        {
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
            // Forwarded headers MUST run first: every downstream check that
            // reads Request.Scheme / IsHttps / RemoteIpAddress (HTTPS
            // redirect, per-IP rate limiting, request logging) needs the
            // real client values rather than the proxy hop. See
            // ForwardedHeadersConfiguration for the trusted-proxy set.
            app.UseForwardedHeaders(ForwardedHeadersConfiguration.Build(app.Configuration));

            app.UseMiddleware<AuthorizationStatusMiddleware>();

            // Defense-in-depth headers go BEFORE CORS / auth so they
            // apply to every response, including preflight 204s, the
            // SPA static bundle, and any 401/403/500 error path.
            app.UseSecurityHeaders(SecurityHeadersOptions.FromConfiguration(app.Configuration));

            app.UseCors(MyAllowSpecificOrigins);
            app.UseMiddleware<ApiClientHeaderMiddleware>();
            // Default-on outside Development. Self-hosted deployments
            // that terminate TLS at a reverse proxy can flip
            // Https:Redirect=false to skip the redirect, but anything
            // not explicitly opted out gets the protection.
            if (app.Configuration.GetValue("Https:Redirect", !app.Environment.IsDevelopment()))
            {
                app.UseHttpsRedirection();
            }

            await ConfigureEnvironment(app);
            app.UseAuthentication();
            app.UseRateLimiter();
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
