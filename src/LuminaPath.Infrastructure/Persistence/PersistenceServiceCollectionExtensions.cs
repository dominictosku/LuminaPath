using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LuminaPath.Infrastructure;

internal static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = GetConnectionString(config);

        services.AddDbContextFactory<LuminaPathDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddScoped<ILuminaPathDbContext, LuminaPathDbContext>();

        return services;
    }

    private static string GetConnectionString(IConfiguration config)
    {
        var explicitConnectionString = ConfigurationValues.FirstNonEmpty(
            config.GetConnectionString("Default"),
            config["POSTGRESQL_DB"]);

        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return explicitConnectionString;
        }

        var host = ConfigurationValues.FirstNonEmpty(config["Database:Host"], config["POSTGRES_HOST"]);
        var database = ConfigurationValues.FirstNonEmpty(config["Database:Name"], config["Database:Database"], config["POSTGRES_DB"]);
        var username = ConfigurationValues.FirstNonEmpty(config["Database:Username"], config["Database:User"], config["POSTGRES_USER"]);
        var password = SecretConfiguration.GetSecret(
            config,
            "Database:Password",
            "Database:PasswordFile",
            ["POSTGRES_PASSWORD"],
            ["POSTGRES_PASSWORD_FILE"]);

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(database)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Missing database connection settings. Set ConnectionStrings__Default, or set Database:Host, Database:Name, Database:Username, and Database:Password or Database:PasswordFile.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = GetDatabasePort(config),
            Database = database,
            Username = username,
            Password = password
        };

        return builder.ConnectionString;
    }

    private static int GetDatabasePort(IConfiguration config)
    {
        var configured = ConfigurationValues.FirstNonEmpty(config["Database:Port"], config["POSTGRES_PORT"]);
        return int.TryParse(configured, out var port)
            ? port
            : 5432;
    }
}
