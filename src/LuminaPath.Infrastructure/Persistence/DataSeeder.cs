using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure
{
    public static class DataSeeder
    {
        /// <summary>
        /// Fallback credentials used only when neither <c>Admin:Email</c>
        /// nor the <c>LUMINAPATH_ADMIN_EMAIL</c> env var is set, AND only
        /// when no admin user exists yet. The password is intentionally
        /// long enough to satisfy the production password policy so a
        /// fresh local deployment doesn't fail at seed time. Production
        /// callers can force real credentials with
        /// <paramref name="requireConfiguredAdminCredentials"/>.
        /// </summary>
        private const string DefaultAdminEmail = "admin@example.com";
        private const string DefaultAdminPassword = "ChangeMe!1AdminAccess";

        public static async Task SeedDatabase(this LuminaPathDbContext context,
            UserManager<LuminaUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration? configuration = null,
            ILogger? logger = null,
            bool requireConfiguredAdminCredentials = false)
        {
            var adminEmail = configuration?["Admin:Email"]
                ?? Environment.GetEnvironmentVariable("LUMINAPATH_ADMIN_EMAIL")
                ?? DefaultAdminEmail;
            var adminPassword = SecretConfiguration.GetSecret(
                    configuration,
                    "Admin:Password",
                    "Admin:PasswordFile",
                    ["LUMINAPATH_ADMIN_PASSWORD"],
                    ["LUMINAPATH_ADMIN_PASSWORD_FILE"])
                ?? DefaultAdminPassword;

            if (string.Equals(adminEmail, DefaultAdminEmail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(adminPassword, DefaultAdminPassword, StringComparison.Ordinal))
            {
                if (requireConfiguredAdminCredentials)
                {
                    throw new InvalidOperationException(
                        "Refusing to seed the bootstrap administrator with bundled default credentials. " +
                        "Set Admin:Email and Admin:Password/Admin:PasswordFile, or LUMINAPATH_ADMIN_EMAIL and LUMINAPATH_ADMIN_PASSWORD/LUMINAPATH_ADMIN_PASSWORD_FILE, before starting a non-development deployment.");
                }

                logger?.LogWarning(
                    "Seeding admin with bundled default credentials ({Email}). " +
                    "Set Admin:Email / Admin:Password or Admin:PasswordFile (or LUMINAPATH_ADMIN_EMAIL / LUMINAPATH_ADMIN_PASSWORD or LUMINAPATH_ADMIN_PASSWORD_FILE) before first boot.",
                    adminEmail);
            }

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
            List<Game> games = new List<Game>()
            {
                new Game
                {
                    Name = "Apex",
                    Description = "Battle Royal",
                    Genres = { "Shooter" },
                    ReleaseDate = new DateTime(2019, 2, 4, 0, 0, 0, DateTimeKind.Utc)
                }
            };
            await context.AddRangeAsync(games);
            await context.SaveChangesAsync();
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in LuminaUserService.Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdminUserAsync(UserManager<LuminaUser> userManager, string adminEmail, string adminPassword)
        {
            // Create the admin user if it doesn't exist
            if (userManager.Users.All(u => u.UserName != adminEmail))
            {
                var adminUser = new LuminaUser
                {
                    FullName = "Admin",
                    UserName = adminEmail,
                    Email = adminEmail,
                    // Keep auto-lockout off for the bootstrap admin so a
                    // brute-force attempt can't lock the only account
                    // that can re-activate others.
                    LockoutEnabled = false,
                    EmailConfirmed = true,
                    // Explicit so this never depends on the C# default
                    // changing later; without it, the seeded admin
                    // could never sign in.
                    IsActive = true
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
