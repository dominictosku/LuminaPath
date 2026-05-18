using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Imports;

internal static class ImportServiceCollectionExtensions
{
    public static IServiceCollection AddImportServices(this IServiceCollection services)
    {
        services.AddScoped<ExcelService>();
        services.AddScoped<GameImportPipeline>();
        return services;
    }
}
