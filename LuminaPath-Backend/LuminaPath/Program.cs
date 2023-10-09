using LuminaPath.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "SPAConfig";

// Add services to the container.
builder.ConfigurateLuminaDatabase();
builder.AddLuminaServices();

builder.Services.AddControllers(options =>
{
	options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});

builder.Services.AddCors(o => o.AddPolicy(MyAllowSpecificOrigins, builder =>
{
	builder.WithOrigins("http://localhost:3000")
		   .WithOrigins("http://127.0.0.1:3000")
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
	await app.MigrateDevelopment();
}
else
{
	await app.MigrateDevelopment(); // Temporary add migrations to Production
	app.UseHsts();
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

app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
