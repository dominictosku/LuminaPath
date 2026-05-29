using LuminaPath;
using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure;
using MudBlazor.Services;
using Radzen;
using Serilog;
using Serilog.Events;
using System.Security.Claims;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "LuminaPath"));

    // Add services to the container.
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddServer()
        .AddBlazor(builder.Environment.IsDevelopment());

    builder.Services.AddMudServices();
    builder.Services.AddRadzenComponents();

    builder.Services.AddScoped<IObjectMapper, ObjectMapper>();

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, _, exception) =>
        {
            if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                return LogEventLevel.Error;
            }

            if (httpContext.Request.Path.StartsWithSegments("/health"))
            {
                return LogEventLevel.Debug;
            }

            return httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
        };
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                diagnosticContext.Set("UserId", userId);
            }
        };
    });

    await app.ConfigureInfrastructure();
    app.ConfigureServer();

    app.UseBlazor();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LuminaPath terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
