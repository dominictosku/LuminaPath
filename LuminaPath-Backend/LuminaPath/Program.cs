using Data;
using Data.Interfaces;
using Data.Models;
using Data.Models.Quests;
using Data.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "MyPolicy";
var connectionstring = builder.Configuration.GetConnectionString("Default");
var Jwt = builder.Configuration.GetSection("Jwt");

// Add services to the container.

builder.Services.AddControllers(options =>
{
	options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});

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


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITokenGenerator, JwtService>();
builder.Services.AddTransient<IGenericCrud<Game>, GenericCrud<Game>>();
builder.Services.AddTransient<IGenericCrud<MyGame>, GenericCrud<MyGame>>();
builder.Services.AddTransient<IGenericCrud<GamesQuest>, GenericCrud<GamesQuest>>();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddCors(o => o.AddPolicy(MyAllowSpecificOrigins, builder =>
{
	builder.WithOrigins("http://localhost:3000")
		   .AllowAnyMethod()
		   .AllowAnyHeader()
		   .AllowCredentials();
}));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();

	// For the proxy in docker compose to work correctly
	app.UseHttpsRedirection();
}
else
{
	app.UseHsts();
}

using (var serviceScope = app.Services.CreateScope())
{
	var services = serviceScope.ServiceProvider;
	var context = serviceScope.ServiceProvider.GetRequiredService<LuminaPathDbContext>();

	await context.SeedIdentityDataAsync(services);

	// Migrations
	try
	{
		context.Database.Migrate();
	}
	catch (Exception e)
	{
		Console.WriteLine("Error: ", e);
	}
}

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

app.UseCors("MyPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
