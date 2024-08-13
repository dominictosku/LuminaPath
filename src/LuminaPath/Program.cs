using LuminaPath;
using LuminaPath.UI.Shared;
using LuminaPath.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddServer()
	.AddBlazor()
	.AddUILibrary();

builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

await app.ConfigureInfrastructure();
app.ConfigureServer();

app.UseBlazor();

app.Run();
