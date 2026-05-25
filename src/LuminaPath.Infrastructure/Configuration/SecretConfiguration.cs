using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Configuration;

internal static class SecretConfiguration
{
    public static string? GetSecret(
        IConfiguration? config,
        string valueKey,
        string fileKey,
        string[]? alternateValueKeys = null,
        string[]? alternateFileKeys = null)
    {
        var directValue = ConfigurationValues.FirstNonEmpty(
            ReadSetting(config, valueKey),
            FirstNonEmptySetting(config, alternateValueKeys));

        if (!string.IsNullOrWhiteSpace(directValue))
        {
            return directValue;
        }

        var filePath = ConfigurationValues.FirstNonEmpty(
            ReadSetting(config, fileKey),
            FirstNonEmptySetting(config, alternateFileKeys));

        return string.IsNullOrWhiteSpace(filePath)
            ? null
            : ReadSecretFile(filePath);
    }

    private static string ReadSecretFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Secret file '{filePath}' does not exist.");
        }

        var value = File.ReadAllText(filePath).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Secret file '{filePath}' is empty.");
        }

        return value;
    }

    private static string? FirstNonEmptySetting(IConfiguration? config, string[]? keys)
    {
        if (keys is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            var value = ReadSetting(config, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ReadSetting(IConfiguration? config, string key)
    {
        return ConfigurationValues.FirstNonEmpty(
            config?[key],
            Environment.GetEnvironmentVariable(key),
            Environment.GetEnvironmentVariable(key.Replace(":", "__")));
    }
}
