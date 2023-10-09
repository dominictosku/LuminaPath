using Data.Models;
using Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LuminaPath.Services
{
	public static class LuminaDatabase
	{
		public static void ConfigurateLuminaDatabase(this WebApplicationBuilder builder)
		{
			var connectionstring = builder.Configuration.GetConnectionString("Default");
			var Jwt = builder.Configuration.GetSection("Jwt");
			builder.Services.AddDbContext<LuminaPathDbContext>(options =>
				options.UseMySql(connectionstring, ServerVersion.AutoDetect(connectionstring)));

			builder.Services.AddIdentity<LuminaUser, IdentityRole>(options =>
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
			builder.Services
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
