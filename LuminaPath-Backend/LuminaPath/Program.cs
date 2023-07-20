using Data;
using Data.Interfaces;
using Data.Models;
using Data.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "MyPolicy";
var connectionstring = builder.Configuration.GetConnectionString("Default");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<LuminaPathDbContext>(options =>
		options.UseMySql(connectionstring, ServerVersion.AutoDetect(connectionstring)));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
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


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IGenericCrud<Games>, GenericCrud<Games>>();
builder.Services.AddTransient<IGenericCrud<PersonalGaming>, GenericCrud<PersonalGaming>>();

builder.Services.AddCors(o => o.AddPolicy(MyAllowSpecificOrigins, builder =>
{
	builder.AllowAnyOrigin()
		   .AllowAnyMethod()
		   .AllowAnyHeader();
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
	app.UseCors("MyPolicy");

	// For the proxy in docker compose to work correctly
	app.UseHttpsRedirection();

	using (var serviceScope = app.Services.CreateScope())
	{
		var context = serviceScope.ServiceProvider.GetRequiredService<LuminaPathDbContext>();
		try{
			context.Database.Migrate();
		}
		catch(Exception e)
		{
            Console.WriteLine("Error: ", e);
        }
	}
}
else
{
	app.UseHsts();
}


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
