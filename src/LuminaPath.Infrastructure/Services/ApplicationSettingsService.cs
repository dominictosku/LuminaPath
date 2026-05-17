using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services;

public sealed class ApplicationSettingsService
{
    public const string SteamApiKey = "Steam.ApiKey";
    public const string PsnBearerToken = "PSN.BearerToken";
    public const string MetadataProvider = "Metadata.Provider";
    public const string MetadataIgdbClientId = "Metadata.IgdbClientId";
    public const string MetadataIgdbClientSecret = "Metadata.IgdbClientSecret";
    public const string MetadataRawgApiKey = "Metadata.RawgApiKey";
    public const string NewsEnabled = "News.Enabled";
    public const string NewsCustomRssUrl = "News.CustomRssUrl";

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public ApplicationSettingsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string> GetSteamApiKeyAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(SteamApiKey, cancellationToken);
    }

    public async Task<bool> HasSteamApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetSteamApiKeyAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(value);
    }

    public Task SaveSteamApiKeyAsync(string value, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(SteamApiKey, value.Trim(), cancellationToken);
    }

    public async Task<string> GetPsnBearerTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(PsnBearerToken, cancellationToken);
    }

    public Task SavePsnBearerTokenAsync(string value, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(PsnBearerToken, value.Trim(), cancellationToken);
    }

    public async Task<GameMetadataSettings> GetGameMetadataSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == MetadataProvider
                || setting.Key == MetadataIgdbClientId
                || setting.Key == MetadataIgdbClientSecret
                || setting.Key == MetadataRawgApiKey)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        return new GameMetadataSettings(
            settings.GetValueOrDefault(MetadataProvider) ?? string.Empty,
            settings.GetValueOrDefault(MetadataIgdbClientId) ?? string.Empty,
            settings.GetValueOrDefault(MetadataIgdbClientSecret) ?? string.Empty,
            settings.GetValueOrDefault(MetadataRawgApiKey) ?? string.Empty);
    }

    public async Task<string> GetGameMetadataProviderAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataProvider, cancellationToken);
    }

    public async Task<string> GetIgdbClientIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataIgdbClientId, cancellationToken);
    }

    public async Task<string> GetIgdbClientSecretAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataIgdbClientSecret, cancellationToken);
    }

    public async Task<string> GetRawgApiKeyAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataRawgApiKey, cancellationToken);
    }

    public async Task SaveGameMetadataSettingsAsync(GameMetadataSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(MetadataProvider, settings.Provider.Trim(), cancellationToken);
        await SaveValueAsync(MetadataIgdbClientId, settings.IgdbClientId.Trim(), cancellationToken);
        await SaveValueAsync(MetadataIgdbClientSecret, settings.IgdbClientSecret.Trim(), cancellationToken);
        await SaveValueAsync(MetadataRawgApiKey, settings.RawgApiKey.Trim(), cancellationToken);
    }

    public async Task<bool> GetNewsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(NewsEnabled, cancellationToken);
        return string.IsNullOrWhiteSpace(value) || bool.TryParse(value, out var enabled) && enabled;
    }

    public Task SaveNewsEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(NewsEnabled, enabled.ToString(), cancellationToken);
    }

    public async Task<string> GetNewsCustomRssUrlAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(NewsCustomRssUrl, cancellationToken);
    }

    public Task SaveNewsCustomRssUrlAsync(string value, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(NewsCustomRssUrl, value.Trim(), cancellationToken);
    }

    private async Task<string> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken)
            ?? string.Empty;
    }

    private async Task SaveValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.ApplicationSettings
            .FirstOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (setting is null)
        {
            context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GameMetadataSettings(
    string Provider,
    string IgdbClientId,
    string IgdbClientSecret,
    string RawgApiKey);
