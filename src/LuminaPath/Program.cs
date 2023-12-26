using Infrastructure;
using Server;
using Microsoft.AspNetCore.Components.Authorization;
using LuminaPath.Components.Account;
using LuminaPath.Components;
using Microsoft.AspNetCore.Identity;
using Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddServices(builder.Configuration)
	.AddServer();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddSingleton<IEmailSender<LuminaUser>, IdentityNoOpEmailSender>();

builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

await app.ConfigureServer();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
