using Application;
using Infrastructure;
using LuminaPath;
using Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddServer()
	.AddBlazor();

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddQuickGridEntityFrameworkAdapter();

var app = builder.Build();

await app.ConfigureInfrastructure();
app.ConfigureServer();

app.UseBlazor();

app.Run();
