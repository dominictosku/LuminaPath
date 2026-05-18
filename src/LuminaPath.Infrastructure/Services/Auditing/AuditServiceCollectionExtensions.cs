using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Auditing;

internal static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAuditingServices(this IServiceCollection services)
    {
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditLogService>();
        return services;
    }
}
