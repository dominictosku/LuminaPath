using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Hubs;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.AiChat.Mcp;
using LuminaPath.Infrastructure.Services.AiChat.Tools;
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
using Microsoft.Extensions.Options;
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
            AddAiChat(services, config);
            AddCors(services, config);
            services.AddSignalR();
            services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, UserIdProvider>();
            services.AddOpenApi();
            return services;
        }

        private static void AddAiChat(IServiceCollection services, IConfiguration config)
        {
            services.Configure<AiChatOptions>(config.GetSection(AiChatOptions.SectionName));

            services.Configure<AnthropicOptions>(config.GetSection(AnthropicOptions.SectionName));
            services.PostConfigure<AnthropicOptions>(opts =>
            {
                if (string.IsNullOrWhiteSpace(opts.ApiKey))
                {
                    opts.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                }
            });

            services.Configure<OpenAiOptions>(config.GetSection(OpenAiOptions.SectionName));
            services.PostConfigure<OpenAiOptions>(opts =>
            {
                if (string.IsNullOrWhiteSpace(opts.ApiKey))
                {
                    opts.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                }
                var envBase = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
                if (!string.IsNullOrWhiteSpace(envBase))
                {
                    opts.BaseUrl = envBase;
                }
                var envModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
                if (!string.IsNullOrWhiteSpace(envModel))
                {
                    opts.Model = envModel;
                }
            });

            services.Configure<McpOptions>(config.GetSection(McpOptions.SectionName));
            services.PostConfigure<McpOptions>(opts =>
            {
                foreach (var server in opts.Servers)
                {
                    foreach (var key in server.Env.Keys.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(server.Env[key]))
                        {
                            server.Env[key] = Environment.GetEnvironmentVariable(key);
                        }
                    }
                }
            });

            services.AddHttpClient<AnthropicClient>();
            services.AddHttpClient<OpenAiCompatibleProvider>();

            services.AddSingleton<IAiProvider>(sp =>
            {
                var chatOptions = sp.GetRequiredService<IOptions<AiChatOptions>>().Value;
                return chatOptions.Provider?.ToLowerInvariant() switch
                {
                    "openai" or "ollama" => sp.GetRequiredService<OpenAiCompatibleProvider>(),
                    _ => sp.GetRequiredService<AnthropicClient>(),
                };
            });

            services.AddSingleton<McpHostService>();
            services.AddHostedService(sp => sp.GetRequiredService<McpHostService>());

            services.AddSingleton<IChatTool, ListUpcomingReleasesTool>();
            services.AddSingleton<IChatTool, SearchMyLibraryTool>();
            services.AddSingleton<IChatTool, MyQuestsTool>();
            services.AddSingleton<IChatTool, MyGamingSessionsTool>();
            services.AddSingleton<IChatTool, LibrarySummaryTool>();

            services.AddSingleton<ChatToolRegistry>();
            services.AddScoped<ChatService>();
        }

        private static void AddCors(IServiceCollection services, IConfiguration config)
        {
            var configuredOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? config.GetSection("FrontendUrls").Get<string[]>()
                ?? SplitConfigValue(config["LUMINAPATH_CORS_ORIGINS"])
                ?? SplitConfigValue(config["FrontendUrl"])
                ?? ["http://localhost:4200"];


            services.AddCors(options => options.AddPolicy(MyAllowSpecificOrigins, policy => policy
                .WithOrigins(configuredOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()));
        }

        private static string[]? SplitConfigValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
            var connectionstring = FirstConfiguredValue(config.GetConnectionString("Default"), config["POSTGRESQL_DB"])
                ?? throw new InvalidOperationException("Missing database connection string. Set ConnectionStrings__Default.");
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
                options.Cookie.SameSite = ParseSameSiteMode(config["Auth:CookieSameSite"], SameSiteMode.None);
                options.Cookie.SecurePolicy = ParseCookieSecurePolicy(config["Auth:CookieSecurePolicy"], CookieSecurePolicy.Always);
            });
        }

        private static SameSiteMode ParseSameSiteMode(string? value, SameSiteMode fallback)
        {
            return Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var parsed)
                ? parsed
                : fallback;
        }

        private static CookieSecurePolicy ParseCookieSecurePolicy(string? value, CookieSecurePolicy fallback)
        {
            return Enum.TryParse<CookieSecurePolicy>(value, ignoreCase: true, out var parsed)
                ? parsed
                : fallback;
        }

        private static void AddServices(IServiceCollection services, IConfiguration config)
        {
            AddStorageService(services, config);
            services.AddScoped<GameService>();
            services.AddScoped<MyGameService>();
            services.AddScoped<ExcelService>();
            services.AddScoped<QuestService>();
            services.AddScoped<GamingSessionService>();
            services.AddScoped<DocumentService>();
            services.AddScoped<LuminaUserService>();
            services.AddScoped<FriendsService>();
            services.AddScoped<DirectMessageService>();
            services.AddScoped<ApplicationSettingsService>();
            services.AddTransient<PSNService>();
            AddSteam(services, config);
        }

        private static void AddSteam(IServiceCollection services, IConfiguration config)
        {
            services.Configure<SteamOptions>(config.GetSection(SteamOptions.SectionName));
            services.AddHttpClient<SteamService>();
        }

        private static void AddStorageService(IServiceCollection services, IConfiguration config)
        {
            string provider = config.GetSection("Storage")["Provider"]
                ?? "Azure";

            if (provider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
            {
                string storagePath = config.GetSection("Storage")["Path"]
                    ?? Path.Combine("App_Data", "storage");

                services.AddScoped<IStorageService, FileSystemStorage>(s =>
                {
                    var environment = s.GetRequiredService<IHostEnvironment>();
                    var fullPath = Path.IsPathRooted(storagePath)
                        ? storagePath
                        : Path.Combine(environment.ContentRootPath, storagePath);

                    return new FileSystemStorage(fullPath, s.GetRequiredService<ILogger<FileSystemStorage>>());
                });

                return;
            }

            string connectionString = FirstConfiguredValue(config.GetSection("Azure")["BlobConnectionString"], config["AZURE_CONNECTIONSTRING"])
                ?? throw new Exception("No blob connectionfound");

            string containerName = FirstConfiguredValue(config.GetSection("Azure")["BlobContainerName"], config["AZURE_CONTAINER_NAME"])
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

        private static string? FirstConfiguredValue(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
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
            app.MapHub<DirectMessageHub>("/hubs/messages");
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
            .RequireAuthorization();

            app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "LuminaPath" }))
                .AllowAnonymous();
        }
    }
}
