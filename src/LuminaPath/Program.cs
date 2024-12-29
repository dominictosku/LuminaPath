using LuminaPath;
using LuminaPath.Infrastructure;
using LuminaPath.UI.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "../LuminaPath.WebApp/www";
});

// Add services to the container.
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddServer()
    .AddBlazor()
    .AddUILibrary();

builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

//app.UseSpaStaticFiles();
//app.UseSpa(spa =>
//{
//    spa.Options.SourcePath = "../LuminaPath.WebApp";
//});

await app.ConfigureInfrastructure();
app.ConfigureServer();

app.UseBlazor();

app.Run();
