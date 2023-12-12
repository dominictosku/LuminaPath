using Data.Models;
using Data;
using Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Services
{
	public static class LuminaMigration
	{
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
					Console.WriteLine("Error: ", e);
				}
			}
		}
	}
}
