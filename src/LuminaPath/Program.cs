using LuminaPath;
using Infrastructure;
using Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddServices(builder.Configuration)
	.AddServer()
	.AddBlazor();

builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

app.ConfigureInfrastructure();
await app.ConfigureServer();

app.UseBlazor();

app.Run();
