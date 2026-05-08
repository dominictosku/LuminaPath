using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services;

public sealed class ApplicationSettingsService
{
    public const string SteamApiKey = "Steam.ApiKey";
    public const string PsnBearerToken = "PSN.BearerToken";

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
