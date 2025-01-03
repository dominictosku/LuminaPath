using LuminaPath;
using LuminaPath.Infrastructure;
using MudBlazor.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "../LuminaPath.WebApp/www";
});


// Add services to the container.
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddServer()
    .AddBlazor();

builder.Services.AddMudServices();
builder.Services.AddRadzenComponents();

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
