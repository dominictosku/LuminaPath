using Core;
using Core.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
		{
			AddMySqlDatabase(services, config);
			AddDefaultIdentity(services, config);
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
			services.AddDbContext<LuminaPathDbContext>(options =>
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

		private static void AddCustomBearerIdentity(IServiceCollection services, IConfiguration config)
		{
			var Jwt = config.GetSection("Jwt");
			services.AddIdentity<LuminaUser, IdentityRole>(options =>
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
				options.User.RequireUniqueEmail = false;
			})
				.AddEntityFrameworkStores<LuminaPathDbContext>()
				.AddRoles<IdentityRole>()
				.AddDefaultTokenProviders();

			// Authentication options
			services
				.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters()
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidAudience = Jwt["Audience"],
						ValidIssuer = Jwt["Issuer"],
						IssuerSigningKey = new SymmetricSecurityKey(
							Encoding.UTF8.GetBytes(Jwt["Key"])
						)
					};
					options.Events = new JwtBearerEvents
					{
						OnMessageReceived = context =>
						{
							context.Token = context.Request.Cookies["X-Access-Token"];
							return Task.CompletedTask;
						}
					};
				});
		}
	}
}
