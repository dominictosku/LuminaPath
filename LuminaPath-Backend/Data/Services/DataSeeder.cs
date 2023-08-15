using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Services
{
	public static class DataSeeder
	{
		public static async Task SeedIdentityDataAsync(IServiceProvider serviceProvider)
		{
			await SeedRolesAsync(serviceProvider);

			// Admin user credentials
			string adminEmail = "admin@example.com";
			string adminPassword = "Admin123!"; // Replace with a strong password

			await SeedAdminUserAsync(serviceProvider, adminEmail, adminPassword);
		}

		public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
		{
			var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			// Create roles if they don't exist
			if (!await roleManager.RoleExistsAsync("Administrator"))
			{
				await roleManager.CreateAsync(new IdentityRole("Administrator"));
			}
		}

		public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, string adminEmail, string adminPassword)
		{
			var userManager = serviceProvider.GetRequiredService<UserManager<LuminaUser>>();

			// Create the admin user if it doesn't exist
			if (userManager.Users.All(u => u.UserName != adminEmail))
			{
				var adminUser = new LuminaUser
				{
					UserName = adminEmail,
					Email = adminEmail,
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(adminUser, adminPassword);

				if (result.Succeeded)
				{
					// Assign the "Admin" role to the admin user
					await userManager.AddToRoleAsync(adminUser, "Administrator");
				}
				else
				{
					// Handle error if user creation fails
					throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors)}");
				}
			}
		}
	}
}
