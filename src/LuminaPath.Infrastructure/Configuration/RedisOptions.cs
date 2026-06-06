using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "LuminaPath:";

    internal static RedisOptions FromConfiguration(IConfiguration config)
    {
        var options = new RedisOptions();
        config.GetSection(SectionName).Bind(options);
        ApplyFallbacks(options, config);
        return options;
    }

    internal static void ApplyFallbacks(RedisOptions options, IConfiguration config)
    {
        options.ConnectionString = ConfigurationValues.FirstNonEmpty(
                config[$"{SectionName}:ConnectionString"],
                config.GetConnectionString("Redis"),
                options.ConnectionString,
                "localhost:6379")
            ?? string.Empty;

        options.InstanceName = ConfigurationValues.FirstNonEmpty(
                options.InstanceName,
                "LuminaPath:")
            ?? string.Empty;
    }
}
