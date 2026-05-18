using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure;

internal static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = ConfigurationValues.FirstNonEmpty(config.GetConnectionString("Default"), config["POSTGRESQL_DB"])
            ?? throw new InvalidOperationException("Missing database connection string. Set ConnectionStrings__Default.");

        services.AddDbContextFactory<LuminaPathDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddScoped<ILuminaPathDbContext, LuminaPathDbContext>();

        return services;
    }
}
