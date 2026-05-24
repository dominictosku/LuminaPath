using System.Reflection;
using System.Globalization;
using System.Threading.RateLimiting;
using FluentValidation;
using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Hubs;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Middleware;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ThirdParty;
using LuminaPath.Infrastructure.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.RateLimiting;
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
            services.AddHttpContextAccessor();
            services.AddAuditingServices();
            services.AddPersistence(config);
            services.AddLuminaIdentity(config);
            services.AddApplicationServices(config);
            services.AddThirdPartyIntegrations(config);
            services.AddAiChatServices(config);
            AddCache(services, config);
            AddCors(services, config);
            AddRateLimits(services);
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
                .AllowCredentials()
                // Allowlist the custom header that LuminaSignInManager
                // sets on a 401 from /api/login when the user's account
                // is inactive — without this, the browser hides it from
                // the Angular SPA on cross-origin requests and the
                // tailored "awaiting approval" message can't be shown.
                .WithExposedHeaders(LoginBlockedReason.ResponseHeaderName)));
        }

        private static void AddRateLimits(IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers["Retry-After"] =
                            Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    await context.HttpContext.Response.WriteAsync(
                        "Too many authentication attempts. Please wait and try again.",
                        cancellationToken);
                };

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var profile = GetAuthRateLimitProfile(context.Request);
                    if (profile is null)
                    {
                        return RateLimitPartition.GetNoLimiter("non-auth");
                    }

                    var partitionKey = $"{profile.Name}:{GetClientIp(context)}";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = profile.PermitLimit,
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            Window = profile.Window
                        });
                });
            });
        }

        private static AuthRateLimitProfile? GetAuthRateLimitProfile(HttpRequest request)
        {
            if (!HttpMethods.IsPost(request.Method))
            {
                return null;
            }

            var path = request.Path.Value ?? string.Empty;
            if (IsPath(path, "/api/login")
                || IsPath(path, "/Account/Login")
                || IsPath(path, "/Account/LoginWith2fa")
                || IsPath(path, "/Account/LoginWithRecoveryCode"))
            {
                return new AuthRateLimitProfile("login", 10, TimeSpan.FromMinutes(5));
            }

            if (IsPath(path, "/api/register")
                || IsPath(path, "/Account/Register")
                || IsPath(path, "/Account/ExternalLogin"))
            {
                return new AuthRateLimitProfile("register", 5, TimeSpan.FromHours(1));
            }

            if (IsPath(path, "/api/forgotPassword")
                || IsPath(path, "/api/resendConfirmationEmail")
                || IsPath(path, "/Account/ForgotPassword"))
            {
                return new AuthRateLimitProfile("email", 5, TimeSpan.FromHours(1));
            }

            return null;
        }

        private static bool IsPath(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetClientIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private sealed record AuthRateLimitProfile(string Name, int PermitLimit, TimeSpan Window);

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
                await context.SeedDatabase(userManager, roleManager, app.Configuration, app.Logger);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error when migrating: ", e);
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
            app.UseMiddleware<AuthorizationStatusMiddleware>();

            // Defense-in-depth headers go BEFORE CORS / auth so they
            // apply to every response, including preflight 204s, the
            // SPA static bundle, and any 401/403/500 error path.
            app.UseSecurityHeaders();

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
            app.UseRateLimiter();
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
