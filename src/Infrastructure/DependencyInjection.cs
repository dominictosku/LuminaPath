using Application.Services;
using Domain;
using Domain.Common.Interfaces;
using Domain.Models;
using Domain.Models.Quests;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
		{
			AddMySqlDatabase(services, config);
			AddDefaultIdentity(services, config);
			AddServices(services, config);
			return services;
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

		private static void AddMySqlDatabase(IServiceCollection services, IConfiguration config)
		{
			var connectionstring = config.GetConnectionString("Default");
			services.AddDbContextFactory<LuminaPathDbContext>(options =>
				options.UseMySql(connectionstring, ServerVersion.AutoDetect(connectionstring)));
		}

		private static void AddDefaultIdentity(IServiceCollection services, IConfiguration config)
		{
			services.AddAuthorization();
			services.AddIdentityApiEndpoints<LuminaUser>(options =>
			{
				// Password settings.
				options.Password.RequireDigit = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireNonAlphanumeric = true;
				options.Password.RequireUppercase = true;
				options.Password.RequiredLength = 6;
				options.Password.RequiredUniqueChars = 1;

				// Lockout settings.
				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(120);
				options.Lockout.MaxFailedAccessAttempts = 10;

				// User settings.
				options.SignIn.RequireConfirmedAccount = true;
				options.User.AllowedUserNameCharacters =
				"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
				options.User.RequireUniqueEmail = true;
			})
				.AddRoles<IdentityRole>()
				.AddEntityFrameworkStores<LuminaPathDbContext>()
				.AddSignInManager()
				.AddDefaultTokenProviders();
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
			services.AddTransient<IGenericRepository<GamesQuest>, GenericRepository<GamesQuest>>();
			services.AddScoped<IUnitOfWork, UnitOfWork>();
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

		public static async Task ConfigureInfrastructure(this WebApplication app)
		{
			await ConfigureEnvironment(app);
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapGroup("/api")
				.MapIdentityApi<LuminaUser>();
		}
	}
}
