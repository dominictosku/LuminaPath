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
		public static async Task SeedDatabase(this LuminaPathDbContext context, UserManager<LuminaUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			// Admin user credentials
			string adminEmail = "admin@example.com";
			string adminPassword = "Admin123*";
			await SeedRolesAsync(roleManager);
			await SeedAdminUserAsync(userManager, adminEmail, adminPassword);
			await SeedGamesAsync(context);
		}

		public static async Task SeedGamesAsync(LuminaPathDbContext context)
		{
			if (context.Games.Any())
			{
				return;
			}
			List<Game> games = new List<Game>();
			games.Add(
				new Game()
				{
					Name = "Apex",
					Description = "Battle Royale",
					Genre = "Shooter",
					ReleaseDate = new DateTime(2017, 07, 28),
					Plattforms = Plattforms.Playstation,
					Playtime = 100
				}
			);
			await context.AddRangeAsync(games);
			await context.SaveChangesAsync();
		}

		public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
		{
			// Create roles if they don't exist
			if (!await roleManager.RoleExistsAsync("Administrator"))
			{
				await roleManager.CreateAsync(new IdentityRole("Administrator"));
			}
		}

		public static async Task SeedAdminUserAsync(UserManager<LuminaUser> userManager, string adminEmail, string adminPassword)
		{
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
