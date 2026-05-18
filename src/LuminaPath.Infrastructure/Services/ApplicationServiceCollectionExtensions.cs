using LuminaPath.Infrastructure.Services.Imports;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services;

internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddStorageServices(config);
        services.AddModelServices();
        services.AddImportServices();
        services.AddApplicationFeatureServices(config);
        services.AddBackgroundJobServices(config);
        services.AddScoped<ApplicationSettingsService>();
        return services;
    }
}
