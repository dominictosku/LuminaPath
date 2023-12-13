using Infrastructure;
using Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddServices(builder.Configuration)
	.AddServer();

builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

await app.ConfigureServer();

app.Run();
