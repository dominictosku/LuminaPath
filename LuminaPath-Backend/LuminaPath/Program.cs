using Data;
using Data.Interfaces;
using Data.Models;
using Data.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "MyPolicy";

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<LuminaPathDbContext>(options =>
		options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
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

using (var serviceScope = app.Services.CreateScope())
{
	var context = serviceScope.ServiceProvider.GetRequiredService<LuminaPathDbContext>();
	context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();

	// For the proxy in docker compose to work correctly
	app.UseHttpsRedirection();
}


app.UseAuthorization();

app.MapControllers();
app.UseCors("MyPolicy");

app.Run();
