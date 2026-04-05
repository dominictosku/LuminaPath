using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.Third_Party;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
namespace LuminaPath.Infrastructure
{
    public static class DependencyInjection
    {
        public const string MyAllowSpecificOrigins = "SPAConfig";

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            AddDatabase(services, config);
            AddDefaultIdentity(services, config);
            AddServices(services, config);
            AddCors(services, config);
            services.AddOpenApi();
            return services;
        }

        private static void AddCors(IServiceCollection services, IConfiguration config)
        {
            string frontendUrl = config["FrontendUrl"] ??
                throw new ArgumentException("Missing frontend url in appsettings.");


            services.AddCors(options => options.AddPolicy(MyAllowSpecificOrigins, policy => policy
                .WithOrigins(frontendUrl)
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

        public static async Task MigrateDevelopment(this WebApplication app)
        {
            using (var serviceScope = app.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<LuminaUser>>();
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var context = serviceScope.ServiceProvider.GetRequiredService<LuminaPathDbContext>();

                // Migrations
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
        }

        private static void AddDatabase(IServiceCollection services, IConfiguration config)
        {
            var connectionstring = Environment.GetEnvironmentVariable("POSTGRESQL_DB") 
                ?? config.GetConnectionString("Default");
            services.AddDbContextFactory<LuminaPathDbContext>(options =>
                options.UseNpgsql(connectionstring));
            services.AddScoped<ILuminaPathDbContext, LuminaPathDbContext>();
        }

        private static void AddDefaultIdentity(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthorization();
            services.AddIdentityApiEndpoints<LuminaUser>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(120);
                options.Lockout.MaxFailedAccessAttempts = 10;

                // User settings.
                options.SignIn.RequireConfirmedAccount = false;
                options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<LuminaPathDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
            });
        }

        private static void AddServices(IServiceCollection services, IConfiguration config)
        {
            AddStorageService(services, config);
            services.AddScoped<GameService>();
            services.AddScoped<MyGameService>();
            services.AddScoped<QuestService>();
            services.AddScoped<DocumentService>();
            services.AddScoped<LuminaUserService>();
            services.AddTransient<PSNService>();
            services.AddTransient<FileSystemService>();
        }

        private static void AddStorageService(IServiceCollection services, IConfiguration config)
        {
            string connectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTIONSTRING")
                ?? config.GetSection("Azure")["BlobConnectionString"]
                ?? throw new Exception("No blob connectionfound");

            string containerName = config.GetSection("Azure")["BlobContainerName"]
                ?? throw new Exception("No blob container name found");

            services.AddScoped<IStorageService, AzureStorage>(s =>
                new AzureStorage(connectionString, containerName, s.GetRequiredService<ILogger<AzureStorage>>()));
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
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "LuminaPath Api");
                });
                //app.UseSwaggerUI(options => options.SwaggerEndpoint("/opeanapi/v1.json", "Luminapath")); // for upgrade to dotnet 9
                await app.MigrateDevelopment();
            }
            else
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                await app.MigrateDevelopment(); // Temporary add migrations to Production
                app.UseHsts();
            }
        }

        public static async Task ConfigureInfrastructure(this WebApplication app)
        {
            AddMiddleware(app);

            app.UseCors(MyAllowSpecificOrigins);
            app.UseHttpsRedirection();

            await ConfigureEnvironment(app);
            app.UseAuthentication();
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
            });
        }

        public static void ConfigureServer(this WebApplication app)
        {
            app.MapControllers();
            app.MapGroup("/api")
                .MapIdentityApi<LuminaUser>();
            app.MapPost("/api/logout", async (SignInManager<LuminaUser> signInManager,
                [FromBody] object empty) =>
            {
                if (empty != null)
                {
                    await signInManager.SignOutAsync();
                    return Results.Ok();
                }
                return Results.Unauthorized();
            })
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
